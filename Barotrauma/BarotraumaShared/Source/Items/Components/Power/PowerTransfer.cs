﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Barotrauma.Items.Components
{
    partial class PowerTransfer : Powered
    {

        public List<Connection> PowerConnections { get; private set; }

        private readonly Dictionary<Connection, bool> connectionDirty = new Dictionary<Connection, bool>();

        //a list of connections a given connection is connected to, either directly or via other power transfer components
        private readonly Dictionary<Connection, HashSet<Connection>> connectedRecipients = new Dictionary<Connection, HashSet<Connection>>();

        protected float powerLoad;

        protected bool isBroken;

        public float PowerLoad
        {
            get { return powerLoad; }
            set { powerLoad = value; }
        }

        [Editable, Serialize(true, true, description: "Can the item be damaged if too much power is supplied to the power grid.")]
        public bool CanBeOverloaded
        {
            get;
            set;
        }

        [Editable(MinValueFloat = 1.0f), Serialize(2.0f, true, description:
            "How much power has to be supplied to the grid relative to the load before item starts taking damage. "
            + "E.g. a value of 2 means that the grid has to be receiving twice as much power as the devices in the grid are consuming.")]
        public float OverloadVoltage
        {
            get;
            set;
        }

        [Serialize(0.15f, true, description: "The probability for a fire to start when the item breaks."), Editable(MinValueFloat = 0.0f, MaxValueFloat = 1.0f)]
        public float FireProbability
        {
            get;
            set;
        }

        [Serialize(false, false, description: "Is the item currently overloaded. Intended to be used by StatusEffect conditionals (setting the value from XML is not recommended).")]
        public bool Overload
        {
            get;
            set;
        }

        //can the component transfer power
        private bool canTransfer;
        public bool CanTransfer
        {
            get { return canTransfer; }
            set
            {
                if (canTransfer == value) return;
                canTransfer = value;
                SetAllConnectionsDirty();
            }
        }

        public override bool IsActive
        {
            get
            {
                return base.IsActive;
            }

            set
            {
                if (base.IsActive == value) return;
                base.IsActive = value;
                powerLoad = 0.0f;
                currPowerConsumption = 0.0f;

                SetAllConnectionsDirty();
                if (!base.IsActive)
                {
                    //we need to refresh the connections here because Update won't be called on inactive components
                    RefreshConnections();
                }
            }
        }

        public PowerTransfer(Item item, XElement element)
            : base(item, element)
        {
            IsActive = true;
            canTransfer = true;

            InitProjectSpecific(element);
        }
        
        partial void InitProjectSpecific(XElement element);

        public override void UpdateBroken(float deltaTime, Camera cam)
        {
            base.UpdateBroken(deltaTime, cam);

            Overload = false;

            if (!isBroken)
            {
                powerLoad = 0.0f;
                currPowerConsumption = 0.0f;
                SetAllConnectionsDirty();
                RefreshConnections();
                isBroken = true;
            }
        }

        public override void Update(float deltaTime, Camera cam)
        {
            RefreshConnections();

            if (!CanTransfer) { return; }

            if (isBroken)
            {
                SetAllConnectionsDirty();
                isBroken = false;
            }

            ApplyStatusEffects(ActionType.OnActive, deltaTime, null);

            //if the item can't be fixed, don't allow it to break
            if (!item.Repairables.Any() || !CanBeOverloaded) { return; }

            float maxOverVoltage = Math.Max(OverloadVoltage, 1.0f);
            Overload = -currPowerConsumption > Math.Max(powerLoad, 200.0f) * maxOverVoltage;
            if (Overload && (GameMain.NetworkMember == null || GameMain.NetworkMember.IsServer))
            {
                //damage the item if voltage is too high (except if running as a client)
                float prevCondition = item.Condition;
                item.Condition -= deltaTime * 10.0f;

                if (item.Condition <= 0.0f && prevCondition > 0.0f)
                {
#if CLIENT
                    SoundPlayer.PlaySound("zap", item.WorldPosition, hullGuess: item.CurrentHull);
                    Vector2 baseVel = Rand.Vector(300.0f);
                    for (int i = 0; i < 10; i++)
                    {
                        var particle = GameMain.ParticleManager.CreateParticle("spark", item.WorldPosition,
                            baseVel + Rand.Vector(100.0f), 0.0f, item.CurrentHull);
                        if (particle != null) particle.Size *= Rand.Range(0.5f, 1.0f);
                    }
#endif
                    float currentIntensity = GameMain.GameSession?.EventManager != null ?
                        GameMain.GameSession.EventManager.CurrentIntensity : 0.5f;

                    //higher probability for fires if the current intensity is low
                    if (FireProbability > 0.0f &&
                        Rand.Range(0.0f, 1.0f) < MathHelper.Lerp(FireProbability, FireProbability * 0.1f, currentIntensity))
                    {
                        new FireSource(item.WorldPosition);
                    }
                }
            }
        }

        public override bool Pick(Character picker)
        {
            return picker != null;
        }

        protected void RefreshConnections()
        {
            var connections = item.Connections;
            foreach (Connection c in connections)
            {
                if (!connectionDirty.ContainsKey(c))
                {
                    connectionDirty[c] = true;
                }
                else if (!connectionDirty[c])
                {
                    continue;
                }

                HashSet<Connection> connected = new HashSet<Connection>();
                if (!connectedRecipients.ContainsKey(c))
                {
                    connectedRecipients.Add(c, connected);
                }
                else
                {
                    //mark all previous recipients as dirty
                    foreach (Connection recipient in connectedRecipients[c])
                    {
                        var pt = recipient.Item.GetComponent<PowerTransfer>();
                        if (pt != null) pt.connectionDirty[recipient] = true;
                    }
                }

                //find all connections that are connected to this one (directly or via another PowerTransfer)
                connected.Add(c);
                GetConnected(c, connected);
                connectedRecipients[c] = connected;

                //go through all the PowerTransfers and we're connected to and set their connections to match the ones we just calculated
                //(no need to go through the recursive GetConnected method again)
                foreach (Connection recipient in connected)
                {
                    var recipientPowerTransfer = recipient.Item.GetComponent<PowerTransfer>();
                    if (recipientPowerTransfer == null) continue;

                    if (!connectedRecipients.ContainsKey(recipient))
                    {
                        connectedRecipients.Add(recipient, connected);
                    }

                    recipientPowerTransfer.connectedRecipients[recipient] = connected;
                    recipientPowerTransfer.connectionDirty[recipient] = false;
                }
            }
        }

        //Finds all the connections that can receive a signal sent into the given connection and stores them in the hashset.
        private void GetConnected(Connection c, HashSet<Connection> connected)
        {
            var recipients = c.Recipients;

            foreach (Connection recipient in recipients)
            {
                if (recipient == null || connected.Contains(recipient)) continue;

                Item it = recipient.Item;
                if (it == null || it.Condition <= 0.0f) continue;

                connected.Add(recipient);

                var powerTransfer = it.GetComponent<PowerTransfer>();
                if (powerTransfer != null && powerTransfer.CanTransfer && powerTransfer.IsActive)
                {
                    GetConnected(recipient, connected);
                }
            }
        }

        public void SetAllConnectionsDirty()
        {
            if (item.Connections == null) return;
            foreach (Connection c in item.Connections)
            {
                connectionDirty[c] = true;
            }
        }

        public void SetConnectionDirty(Connection connection)
        {
            var connections = item.Connections;
            if (connections == null || !connections.Contains(connection)) return;
            connectionDirty[connection] = true;
        }

        public override void OnItemLoaded()
        {
            base.OnItemLoaded();
            var connections = Item.Connections;
            PowerConnections = connections == null ? new List<Connection>() : connections.FindAll(c => c.IsPower);  
            if (connections == null)
            {
                IsActive = false;
                return;
            }
            SetAllConnectionsDirty();
        }

        public override void ReceivePowerProbeSignal(Connection connection, Item source, float power)
        {
            //we've already received this signal
            if (lastPowerProbeRecipients.Contains(this)) { return; }
            lastPowerProbeRecipients.Add(this);

            if (power < 0.0f)
            {
                powerLoad -= power;
            }
            else
            {
                currPowerConsumption -= power;
            }
            powerOut?.SendPowerProbeSignal(source, power);
        }

        public override void ReceiveSignal(int stepsTaken, string signal, Connection connection, Item source, Character sender, float power, float signalStrength = 1.0f)
        {
            if (item.Condition <= 0.0f) { return; }
            if (connection.IsPower)
            {
                /*if (!updatingPower) { return; }

                //we've already received this signal
                for (int i = 0; i<source.LastSentSignalRecipients.Count -1;i++)
                {
                    if (source.LastSentSignalRecipients[i] == item) { return; }
                }

                if (power < 0.0f)
                {
                    powerLoad -= power;
                }
                else
                {
                    currPowerConsumption -= power;
                }
                connection.SendSignal(stepsTaken, signal, source, sender, power, signalStrength);*/
                return;
            }

            base.ReceiveSignal(stepsTaken, signal, connection, source, sender, power);

            if (!connectedRecipients.ContainsKey(connection)) return;

            if (connection.Name.Length > 5 && connection.Name.Substring(0, 6) == "signal")
            {
                foreach (Connection recipient in connectedRecipients[connection])
                {
                    if (recipient.Item == item || recipient.Item == source) continue;

                    foreach (ItemComponent ic in recipient.Item.Components)
                    {
                        //powertransfer components don't need to receive the signal in the pass-through signal connections
                        //because we relay it straight to the connected items without going through the whole chain of junction boxes
                        if (ic is PowerTransfer && connection.Name.Contains("signal")) continue;
                        ic.ReceiveSignal(stepsTaken, signal, recipient, source, sender, 0.0f, signalStrength);
                    }

                    bool broken = recipient.Item.Condition <= 0.0f;
                    foreach (StatusEffect effect in recipient.Effects)
                    {
                        if (broken && effect.type != ActionType.OnBroken) continue;
                        recipient.Item.ApplyStatusEffect(effect, ActionType.OnUse, 1.0f, null, null, false, false);
                    }
                }
            }
        }
    }
}
