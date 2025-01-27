﻿using System;
using System.Collections.Generic;
using System.Text;
using MoonSharp.Interpreter;
using Microsoft.Xna.Framework;
using Barotrauma.Networking;
using Barotrauma.Items.Components;
using System.IO;
using System.Net;
using System.Linq;
using System.Xml.Linq;
using FarseerPhysics.Dynamics;
using System.Reflection;
using HarmonyLib;
using MoonSharp.Interpreter.Interop;
using System.Diagnostics;

namespace Barotrauma
{
	public partial class LuaTimer
	{
		public static long LastUpdateTime = 0;

		public static double Time
		{
			get
			{
				return GetTime();
			}
		}

		public void Wait(object function, int millisecondDelay)
		{
			GameMain.Lua.hook.EnqueueTimedFunction((float)Timing.TotalTime + (millisecondDelay / 1000f), function);
		}

		public static double GetTime()
		{
			return Timing.TotalTime;
		}

		public static float GetUsageMemory()
		{
			Process proc = Process.GetCurrentProcess();
			float memory = MathF.Round(proc.PrivateMemorySize64 / (1024 * 1024), 2);
			proc.Dispose();

			return memory;
		}
	}

	partial class LuaFile
	{
		// TODO: SANDBOXING

		public static bool IsPathAllowed(string path)
		{
			path = Path.GetFullPath(path).CleanUpPath();

			if (path.StartsWith(Path.GetFullPath("Mods").CleanUpPath()))
				return true;

			if (path.StartsWith(Path.GetFullPath("Submarines").CleanUpPath()))
				return true;

			if (path.StartsWith(Path.GetFullPath("Data").CleanUpPath()))
				return true;

			if (path.StartsWith(Path.GetFullPath("Lua").CleanUpPath()))
				return true;

			if (path.StartsWith(Path.GetFullPath("Content").CleanUpPath()))
				return true;

			return false;
		}

		public static bool IsPathAllowedLuaException(string path)
		{
			if (IsPathAllowed(path))
				return true;
			else
				GameMain.Lua.HandleLuaException(new Exception("File access to \"" + path + "\" not allowed."));

			return false;
		}

		public static string Read(string path)
		{
			if (!IsPathAllowedLuaException(path))
				return "";

			return File.ReadAllText(path);
		}

		public static void Write(string path, string text)
		{
			if (!IsPathAllowedLuaException(path))
				return;

			File.WriteAllText(path, text);
		}

		public static bool Exists(string path)
		{
			if (!IsPathAllowedLuaException(path))
				return false;

			return File.Exists(path);
		}

		public static bool CreateDirectory(string path)
		{
			if (!IsPathAllowedLuaException(path))
				return false;

			Directory.CreateDirectory(path);

			return true;
		}

		public static bool DirectoryExists(string path)
		{
			if (!IsPathAllowedLuaException(path))
				return false;

			return Directory.Exists(path);
		}

		public static string[] GetFiles(string path)
		{
			return Directory.GetFiles(path);
		}

		public static string[] GetDirectories(string path)
		{
			if (!IsPathAllowedLuaException(path))
				return new string[] { };

			return Directory.GetDirectories(path);
		}

		public static string[] DirSearch(string sDir)
		{
			if (!IsPathAllowedLuaException(sDir))
				return new string[] { };

			List<string> files = new List<string>();

			try
			{
				foreach (string f in Directory.GetFiles(sDir))
				{
					files.Add(f);
				}

				foreach (string d in Directory.GetDirectories(sDir))
				{
					foreach (string f in Directory.GetFiles(d))
					{
						files.Add(f);
					}
					DirSearch(d);
				}
			}
			catch (System.Exception excpt)
			{
				Console.WriteLine(excpt.Message);
			}

			return files.ToArray();
		}
	}

	partial class LuaNetworking
	{
		public bool restrictMessageSize = true;

		public Dictionary<string, object> LuaNetReceives = new Dictionary<string, object>();

#if SERVER
		[MoonSharpHidden]
		public void NetMessageReceived(IReadMessage netMessage, ClientPacketHeader header, Client client = null)
		{
			if (header == ClientPacketHeader.LUA_NET_MESSAGE)
			{
				string netMessageName = netMessage.ReadString();
				if (LuaNetReceives[netMessageName] is Closure)
					GameMain.Lua.CallFunction(LuaNetReceives[netMessageName], new object[] { netMessage, client });
			}
			else
			{
				GameMain.Lua.hook.Call("netMessageReceived", netMessage, header, client);
			}
		}

#else
			[MoonSharpHidden]
			public void NetMessageReceived(IReadMessage netMessage, ServerPacketHeader header, Client client = null)
			{
				if (header == ServerPacketHeader.LUA_NET_MESSAGE)
				{
					string netMessageName = netMessage.ReadString();
					if (LuaNetReceives[netMessageName] is Closure)
					GameMain.Lua.lua.Call(LuaNetReceives[netMessageName], new object[] { netMessage, client });
				}
				else
				{
					GameMain.Lua.hook.Call("netMessageReceived", netMessage, header, client);
				}
			}
#endif

		public void Receive(string netMessageName, object callback)
		{
			LuaNetReceives[netMessageName] = callback;
		}

		public IWriteMessage Start(string netMessageName)
		{
			var message = new WriteOnlyMessage();
#if SERVER
			message.Write((byte)ServerPacketHeader.LUA_NET_MESSAGE);
#else
				message.Write((byte)ClientPacketHeader.LUA_NET_MESSAGE);
#endif
			message.Write(netMessageName);
			return ((IWriteMessage)message);
		}

		public IWriteMessage Start()
		{
			return new WriteOnlyMessage();
		}

#if SERVER
		public void ClientWriteLobby(Client client) => GameMain.Server.ClientWriteLobby(client);

		public void Send(IWriteMessage netMessage, NetworkConnection connection = null, DeliveryMethod deliveryMethod = DeliveryMethod.Reliable)
		{
			if (connection == null)
			{
				foreach (NetworkConnection conn in Client.ClientList.Select(c => c.Connection))
				{
					GameMain.Server.ServerPeer.Send(netMessage, conn, deliveryMethod);
				}
			}
			else
			{
				GameMain.Server.ServerPeer.Send(netMessage, connection, deliveryMethod);
			}
		}
#else
			public void Send(IWriteMessage netMessage, DeliveryMethod deliveryMethod = DeliveryMethod.Reliable)
			{
				GameMain.Client.ClientPeer.Send(netMessage, deliveryMethod);
			}
#endif

		public void RequestPostHTTP(string url, object callback, string data, string contentType = "application/json")
		{
			try
			{
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
				httpWebRequest.ContentType = contentType;
				httpWebRequest.Method = "POST";

				using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
					streamWriter.Write(data);

				httpWebRequest.BeginGetResponse(new AsyncCallback((IAsyncResult result) =>
				{
					try
					{
						var httpResponse = httpWebRequest.EndGetResponse(result);
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
							GameMain.Lua.hook.EnqueueFunction(callback, streamReader.ReadToEnd());
					}
					catch (Exception e)
					{
						GameMain.Lua.hook.EnqueueFunction(callback, e.ToString());
					}
				}), null);

			}
			catch (Exception e)
			{
				GameMain.Lua.hook.EnqueueFunction(callback, e.ToString());
			}
		}

		public void RequestGetHTTP(string url, object callback)
		{
			try
			{
				var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

				httpWebRequest.BeginGetResponse(new AsyncCallback((IAsyncResult result) =>
				{
					try
					{
						var httpResponse = httpWebRequest.EndGetResponse(result);
						using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
							GameMain.Lua.hook.EnqueueFunction(callback, streamReader.ReadToEnd());
					}
					catch (Exception e)
					{
						GameMain.Lua.hook.EnqueueFunction(callback, e.ToString());
					}
				}), null);
			}
			catch (Exception e)
			{
				GameMain.Lua.hook.EnqueueFunction(callback, e.ToString());
			}
		}

		public void CreateEntityEvent(INetSerializable entity, object[] extraData)
		{
			GameMain.NetworkMember.CreateEntityEvent(entity, extraData);
		}

#if SERVER
		public void UpdateClientPermissions(Client client)
		{
			GameMain.Server.UpdateClientPermissions(client);
		}

		public void RemovePendingClient(ServerPeer.PendingClient pendingClient, DisconnectReason reason, string msg)
		{
			GameMain.Server.ServerPeer.RemovePendingClient(pendingClient, reason, msg);
		}
#endif


		public ushort LastClientListUpdateID
		{
			get { return GameMain.NetworkMember.LastClientListUpdateID; }
			set { GameMain.NetworkMember.LastClientListUpdateID = value; }
		}
	}

}

public class LuaResult
{
	object result;
	public LuaResult(object arg)
	{
		result = arg;
	}

	public bool IsNull()
	{
		if (result == null)
			return true;

		if (result is DynValue dynValue)
			return dynValue.IsNil();

		return false;
	}

	public bool Bool()
	{
		if (result is DynValue dynValue)
		{
			return dynValue.CastToBool();
		}

		return false;
	}

	public float Float()
	{
		if (result is DynValue dynValue)
		{
			var num = dynValue.CastToNumber();
			if (num == null) { return 0f; }
			return (float)num.Value;
		}

		return 0f;
	}

	public double Double()
	{
		if (result is DynValue dynValue)
		{
			var num = dynValue.CastToNumber();
			if (num == null) { return 0f; }
			return num.Value;
		}

		return 0f;
	}

	public string String()
	{
		if (result is DynValue dynValue)
		{
			var str = dynValue.CastToString();
			if (str == null) { return ""; }
			return str;
		}

		return "";
	}

	public object Object()
	{
		if (result is DynValue dynValue)
		{
			return dynValue.ToObject();
		}

		return null;
	}
}