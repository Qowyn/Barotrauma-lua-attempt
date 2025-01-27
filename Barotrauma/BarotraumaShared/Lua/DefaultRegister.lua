local function RegisterBarotrauma(typeName)
    return LuaUserData.RegisterType("Barotrauma." .. typeName)
end

LuaUserData.RegisterType("System.TimeSpan")

if SERVER then
    RegisterBarotrauma("Networking.GameServer")
end

RegisterBarotrauma("LuaByte")
RegisterBarotrauma("LuaUShort")
RegisterBarotrauma("LuaFloat")

RegisterBarotrauma("CauseOfDeathType")
RegisterBarotrauma("Level+InterestingPosition")
RegisterBarotrauma("Level+PositionType")
RegisterBarotrauma("Networking.TraitorMessageType")
RegisterBarotrauma("SpawnType")
RegisterBarotrauma("Networking.ChatMessageType")
RegisterBarotrauma("InputType")

RegisterBarotrauma("Job")
RegisterBarotrauma("JobPrefab")
RegisterBarotrauma("Level")
RegisterBarotrauma("Networking.ServerLog+MessageType")
RegisterBarotrauma("WayPoint")
RegisterBarotrauma("Character")
RegisterBarotrauma("Item")
RegisterBarotrauma("Submarine")
RegisterBarotrauma("Networking.Client")
RegisterBarotrauma("Networking.NetworkConnection")
RegisterBarotrauma("Networking.LidgrenConnection")
RegisterBarotrauma("Networking.SteamP2PConnection")
RegisterBarotrauma("AfflictionPrefab")
RegisterBarotrauma("Affliction")
RegisterBarotrauma("CharacterHealth")
RegisterBarotrauma("AnimController")
RegisterBarotrauma("Limb")
RegisterBarotrauma("Ragdoll")
RegisterBarotrauma("Networking.ChatMessage")
RegisterBarotrauma("CharacterHealth+LimbHealth")
RegisterBarotrauma("AttackResult")
RegisterBarotrauma("Entity")
RegisterBarotrauma("EntitySpawner")
RegisterBarotrauma("MapEntity")
RegisterBarotrauma("MapEntityPrefab")
RegisterBarotrauma("CauseOfDeath")
RegisterBarotrauma("CharacterTeamType")
RegisterBarotrauma("Hull")
RegisterBarotrauma("Gap")
RegisterBarotrauma("PhysicsBody")
RegisterBarotrauma("InvSlotType")
RegisterBarotrauma("LimbType")
RegisterBarotrauma("ActionType")
RegisterBarotrauma("ItemPrefab")
RegisterBarotrauma("SerializableProperty")

RegisterBarotrauma("StatusEffect")
RegisterBarotrauma("FireSource")
RegisterBarotrauma("ContentPackage")
RegisterBarotrauma("SubmarineBody")
RegisterBarotrauma("Explosion")
RegisterBarotrauma("Networking.ServerSettings")
RegisterBarotrauma("Networking.ServerSettings+SavedClientPermission")
RegisterBarotrauma("Inventory")
RegisterBarotrauma("ItemInventory")
RegisterBarotrauma("CharacterInventory")
RegisterBarotrauma("Inventory+ItemSlot")
RegisterBarotrauma("FireSource")

RegisterBarotrauma("Items.Components.Connection")
RegisterBarotrauma("Items.Components.Fabricator")
RegisterBarotrauma("Items.Components.ItemComponent")
RegisterBarotrauma("Items.Components.WifiComponent")
RegisterBarotrauma("Items.Components.LightComponent")
RegisterBarotrauma("Items.Components.Holdable")
RegisterBarotrauma("Items.Components.CustomInterface")
RegisterBarotrauma("Items.Components.CustomInterface+CustomInterfaceElement")
RegisterBarotrauma("Items.Components.ItemContainer")
RegisterBarotrauma("Items.Components.PowerContainer")
RegisterBarotrauma("Items.Components.Pickable")
RegisterBarotrauma("Items.Components.Reactor")
RegisterBarotrauma("Items.Components.RelayComponent")
RegisterBarotrauma("Items.Components.MemoryComponent")
RegisterBarotrauma("Items.Components.Engine")
RegisterBarotrauma("Items.Components.Growable")
RegisterBarotrauma("Items.Components.MeleeWeapon")
RegisterBarotrauma("Items.Components.IdCard")
RegisterBarotrauma("Items.Components.Steering")
RegisterBarotrauma("Items.Components.Wire")
RegisterBarotrauma("Items.Components.Turret")
RegisterBarotrauma("Items.Components.Sprayer")
RegisterBarotrauma("Items.Components.SonarTransducer")
RegisterBarotrauma("Items.Components.Powered")
RegisterBarotrauma("Items.Components.PowerTransfer")
RegisterBarotrauma("Items.Components.Planter")
RegisterBarotrauma("Items.Components.OxygenGenerator")
RegisterBarotrauma("Items.Components.OutpostTerminal")
RegisterBarotrauma("Items.Components.Ladder")
RegisterBarotrauma("Items.Components.ElectricalDischarger")
RegisterBarotrauma("Items.Components.Door")
RegisterBarotrauma("Items.Components.DockingPort")
RegisterBarotrauma("Items.Components.Deconstructor")
RegisterBarotrauma("Items.Components.Connection")
RegisterBarotrauma("Items.Components.ConnectionPanel")
RegisterBarotrauma("Items.Components.GeneticMaterial")
RegisterBarotrauma("Items.Components.GrowthSideExtension")
RegisterBarotrauma("Items.Components.ButtonTerminal")
RegisterBarotrauma("Items.Components.Propulsion")
RegisterBarotrauma("Items.Components.Pump")
RegisterBarotrauma("Items.Components.RangedWeapon")
RegisterBarotrauma("Items.Components.Terminal")
RegisterBarotrauma("Items.Components.Throwable")
RegisterBarotrauma("Items.Components.Wearable")
RegisterBarotrauma("Items.Components.SmokeDetector")
RegisterBarotrauma("Items.Components.Repairable")
RegisterBarotrauma("Items.Components.RepairTool")
RegisterBarotrauma("Items.Components.NameTag")
RegisterBarotrauma("Items.Components.LevelResource")
RegisterBarotrauma("Items.Components.EntitySpawnerComponent")

RegisterBarotrauma("AIController")
RegisterBarotrauma("EnemyAIController")
RegisterBarotrauma("HumanAIController")
RegisterBarotrauma("AICharacter")
RegisterBarotrauma("AITarget")
RegisterBarotrauma("AITargetMemory")
RegisterBarotrauma("AIChatMessage")
RegisterBarotrauma("AIObjectiveManager")
RegisterBarotrauma("AITrigger")
RegisterBarotrauma("WreckAI")
RegisterBarotrauma("WreckAIConfig")

RegisterBarotrauma("AIObjectiveChargeBatteries")
RegisterBarotrauma("AIObjective")
RegisterBarotrauma("AIObjectiveCleanupItem")
RegisterBarotrauma("AIObjectiveCleanupItems")
RegisterBarotrauma("AIObjectiveCombat")
RegisterBarotrauma("AIObjectiveContainItem")
RegisterBarotrauma("AIObjectiveDecontainItem")
RegisterBarotrauma("AIObjectiveEscapeHandcuffs")
RegisterBarotrauma("AIObjectiveExtinguishFire")
RegisterBarotrauma("AIObjectiveExtinguishFires")
RegisterBarotrauma("AIObjectiveFightIntruders")
RegisterBarotrauma("AIObjectiveFindDivingGear")
RegisterBarotrauma("AIObjectiveFindSafety")
RegisterBarotrauma("AIObjectiveFixLeak")
RegisterBarotrauma("AIObjectiveFixLeaks")
RegisterBarotrauma("AIObjectiveGetItem")
RegisterBarotrauma("AIObjectiveGoTo")
RegisterBarotrauma("AIObjectiveIdle")
RegisterBarotrauma("AIObjectiveOperateItem")
RegisterBarotrauma("AIObjectiveOperateItem")
RegisterBarotrauma("AIObjectivePumpWater")
RegisterBarotrauma("AIObjectiveRepairItem")
RegisterBarotrauma("AIObjectiveRepairItems")
RegisterBarotrauma("AIObjectiveRescue")
RegisterBarotrauma("AIObjectiveRescueAll")
RegisterBarotrauma("AIObjectiveReturn")
RegisterBarotrauma("AIObjectiveCombat+CombatMode")

RegisterBarotrauma("TalentPrefab")
RegisterBarotrauma("TalentOption")
RegisterBarotrauma("TalentSubTree")
RegisterBarotrauma("TalentTree")
RegisterBarotrauma("CharacterTalent")
RegisterBarotrauma("Upgrade")
RegisterBarotrauma("UpgradeCategory")
RegisterBarotrauma("UpgradePrefab")
RegisterBarotrauma("UpgradeManager")

RegisterBarotrauma("Screen")
RegisterBarotrauma("GameScreen")
RegisterBarotrauma("GameSession")
RegisterBarotrauma("GameSettings")
RegisterBarotrauma("CampaignMode")
RegisterBarotrauma("CrewManager")

RegisterBarotrauma("DebugConsole+Command")

RegisterBarotrauma("TextManager")

local descriptor = RegisterBarotrauma("NetLobbyScreen")

if SERVER then
    LuaUserData.MakeFieldAccessible(descriptor, "subs")
end

RegisterBarotrauma("Networking.IWriteMessage")
RegisterBarotrauma("Networking.IReadMessage")
RegisterBarotrauma("Networking.ServerPacketHeader")
RegisterBarotrauma("Networking.ClientPacketHeader")
RegisterBarotrauma("Networking.DeliveryMethod")
RegisterBarotrauma("Networking.DeliveryMethod")
RegisterBarotrauma("Networking.NetEntityEvent")
RegisterBarotrauma("Networking.NetEntityEvent+Type")
RegisterBarotrauma("Networking.INetSerializable")
RegisterBarotrauma("Networking.DisconnectReason")
LuaUserData.RegisterType("Lidgren.Network.NetIncomingMessage")
LuaUserData.RegisterType("Lidgren.Network.NetConnection")
LuaUserData.RegisterType("System.Net.IPEndPoint")
LuaUserData.RegisterType("System.Net.IPAddress")

RegisterBarotrauma("Rand+RandSync")
RegisterBarotrauma("Skill")
RegisterBarotrauma("SkillPrefab")
RegisterBarotrauma("TraitorMissionPrefab")
RegisterBarotrauma("TraitorMissionResult")

LuaUserData.RegisterType("FarseerPhysics.Dynamics.Body")
LuaUserData.RegisterType("FarseerPhysics.Dynamics.World")
LuaUserData.RegisterType("FarseerPhysics.Dynamics.Fixture")
RegisterBarotrauma("Physics")

RegisterBarotrauma("Camera")
RegisterBarotrauma("InputType")
RegisterBarotrauma("Key")

RegisterBarotrauma("PrefabCollection`1[[Barotrauma.ItemPrefab]]")
RegisterBarotrauma("PrefabCollection`1[[Barotrauma.JobPrefab]]")
RegisterBarotrauma("PrefabCollection`1[[Barotrauma.CharacterPrefab]]")
RegisterBarotrauma("PrefabCollection`1[[Barotrauma.AfflictionPrefab]]")
RegisterBarotrauma("PrefabCollection`1[[Barotrauma.TalentPrefab]]")

RegisterBarotrauma("Pair`2[[Barotrauma.JobPrefab],[System.Int32]]")

RegisterBarotrauma("Range`1[System.Single]")

RegisterBarotrauma("CharacterInfo")
RegisterBarotrauma("Items.Components.Signal")
RegisterBarotrauma("SubmarineInfo")

RegisterBarotrauma("MapCreatures.Behavior.BallastFloraBehavior")
RegisterBarotrauma("MapCreatures.Behavior.BallastFloraBranch")

LuaUserData.RegisterType("Microsoft.Xna.Framework.Vector2")
LuaUserData.RegisterType("Microsoft.Xna.Framework.Vector3")
LuaUserData.RegisterType("Microsoft.Xna.Framework.Vector4")
LuaUserData.RegisterType("Microsoft.Xna.Framework.Color")
LuaUserData.RegisterType("Microsoft.Xna.Framework.Point")
LuaUserData.RegisterType("Microsoft.Xna.Framework.Rectangle")

if SERVER then
RegisterBarotrauma("Networking.ServerPeer")
RegisterBarotrauma("Networking.ServerPeer+PendingClient")

RegisterBarotrauma("Traitor")
RegisterBarotrauma("Traitor+TraitorMission")

elseif CLIENT then

RegisterBarotrauma("Networking.ClientPeer")

RegisterBarotrauma("ChatBox")
RegisterBarotrauma("GUICanvas")
RegisterBarotrauma("Anchor")
RegisterBarotrauma("Alignment")
RegisterBarotrauma("Pivot")
RegisterBarotrauma("Key")
RegisterBarotrauma("PlayerInput")

LuaUserData.RegisterType("Microsoft.Xna.Framework.Graphics.Texture2D")
LuaUserData.RegisterType("EventInput.KeyEventArgs")
LuaUserData.RegisterType("Microsoft.Xna.Framework.Input.Keys")

RegisterBarotrauma("Sprite")
RegisterBarotrauma("GUILayoutGroup")
RegisterBarotrauma("GUITextBox")
RegisterBarotrauma("GUITextBlock")
RegisterBarotrauma("GUIButton")
RegisterBarotrauma("RectTransform")
RegisterBarotrauma("GUIFrame")
RegisterBarotrauma("GUITickBox")
RegisterBarotrauma("GUICustomComponent")
RegisterBarotrauma("GUIImage")
RegisterBarotrauma("GUIListBox")
RegisterBarotrauma("GUIScrollBar")
RegisterBarotrauma("GUIDropDown")

end