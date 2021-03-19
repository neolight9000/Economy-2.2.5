using CommandHandler;
using Ini;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Economy
{
    class EconomyCore
    {
        public static Dictionary<string, float> wallets = new Dictionary<string, float>();
        public static Dictionary<string, ReceivedData> receiverAnnounce = new Dictionary<string, ReceivedData>();
        public static Dictionary<string, float> ItemPrices = new Dictionary<string, float>();
        public static Dictionary<string, float> VehiclePrices = new Dictionary<string, float>();
        public static string CoinSymbol = "$";
        public static float SellIndex = 0.1f;
        public static bool SellVehicles = true;
        public static float DeathIndex = 0.1f;
        public static float ServerBalance { get; set; }
        public static void _onPlayerConnected(NetworkPlayer player)
        {

            Thread threaded_event = new Thread(new ParameterizedThreadStart(onPlayerConnectedThreaded));
            threaded_event.Start(player);
        }

        private static void onPlayerConnectedThreaded(object player)
        {
            Thread.Sleep(350);
            NetworkUser nplayer = NetworkUserList.getUserFromPlayer((NetworkPlayer)player);
            if (receiverAnnounce.ContainsKey(nplayer.string_3))
            {
                Reference.Tell(nplayer.networkPlayer_0, $"You successfully received {receiverAnnounce[nplayer.string_3].receivedAmount} {CoinSymbol}, from {receiverAnnounce[nplayer.string_3].senderName}");
                receiverAnnounce.Remove(nplayer.string_3);
            }
        }
        public static void SaveWallets()
        {
            StringBuilder sb = new StringBuilder();
            if (!Directory.Exists("Unturned_Data/Managed/mods/Economy/"))
                Directory.CreateDirectory("Unturned_Data/Managed/mods/Economy/");
            if (!File.Exists("Unturned_Data/Managed/mods/Economy/wallets.dat"))
            {
                sb.AppendLine("Server Balance=100000000");
                sb.AppendLine("76561198825589906=0");
                File.WriteAllText("Unturned_Data/Managed/mods/Economy/wallets.dat", sb.ToString());
                return;
            }
            sb = new StringBuilder();
            sb.AppendLine($"Server Balance={ServerBalance}");
            foreach (var kv in wallets)
            {
                sb.AppendLine($"{kv.Key}={kv.Value}");
            }
            var text = sb.ToString();
            File.WriteAllText("Unturned_Data/Managed/mods/Economy/wallets.dat", text);
        }
        public static void CalculateIndex()
        {
            try
            {
                float AllMoney = 0;
                foreach(var wallet in wallets)
                {
                    AllMoney += wallet.Value;
                }
                float averageplayerbalance = AllMoney / wallets.Count;
                var MarketIndex = averageplayerbalance / 20000;
                MarketIndex = MarketIndex > 0.99 ? 0.99f : MarketIndex;
                MarketIndex = MarketIndex < 0.1 ? 0.1f : MarketIndex;
                SellIndex = 1 - MarketIndex;
                var sb = new StringBuilder();
                sb.AppendLine("[Config]");
                sb.AppendLine($"Coin Symbol={CoinSymbol}");
                sb.AppendLine($"Sell Vehicles={SellVehicles}");
                sb.AppendLine($"Sell Index={SellIndex}");
                sb.AppendLine($"Death Index={DeathIndex}");
                var text = sb.ToString();
                File.WriteAllText("Unturned_Data/Managed/mods/Economy/config.ini", text);
            }
            catch { }
        }
        public static void LoadWallets()
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                if (!Directory.Exists("Unturned_Data/Managed/mods/Economy/"))
                    Directory.CreateDirectory("Unturned_Data/Managed/mods/Economy/");
                if (!File.Exists("Unturned_Data/Managed/mods/Economy/wallets.dat"))
                {
                    sb.AppendLine("Server Balance=100000000");
                    sb.AppendLine("76561198825589906=0");
                    File.WriteAllText("Unturned_Data/Managed/mods/Economy/wallets.dat", sb.ToString());
                }
                var lines = File.ReadAllLines("Unturned_Data/Managed/mods/Economy/wallets.dat");
                foreach (var line in lines)
                {
                    if(line.Split('=')[0] == "Server Balance")
                    {
                        ServerBalance = float.Parse(line.Split('=')[1]);
                        continue;
                    }
                    var tokens = line.Split('=');
                    var steamid = tokens[0];
                    var balance = Convert.ToSingle(tokens[1]);
                    wallets.Add(steamid, balance);
                }
            }
            catch { return; }

        }
        public static void LoadConfig()
        {
            if (!Directory.Exists("Unturned_Data/Managed/mods/Economy/"))
                Directory.CreateDirectory("Unturned_Data/Managed/mods/Economy/");
            try
            {
                if (!File.Exists("Unturned_Data/Managed/mods/Economy/config.ini"))
                {
                    IniFile config = new IniFile("Unturned_Data/Managed/mods/Economy/config.ini");
                    config.IniWriteValue("Config", "Coin Symbol", "$");
                    config.IniWriteValue("Config", "Sell Vehicles", "false");
                    config.IniWriteValue("Config", "Sell Index", "0.9");
                    config.IniWriteValue("Config", "Death Index", "0.5");
                }
                string[] confs = File.ReadAllLines("Unturned_Data/Managed/mods/Economy/config.ini");
                CoinSymbol = confs[1].Substring(12);
                SellVehicles = Convert.ToBoolean(confs[2].Substring(14));
                SellIndex = Convert.ToSingle(confs[3].Substring(11));
                DeathIndex = float.Parse(confs[4].Split('=')[1]);
            }
            catch
            {

            }
        }
        public static void LoadPrices()
        {
            try
            {
                if (!Directory.Exists("Unturned_Data/Managed/mods/Economy/"))
                    Directory.CreateDirectory("Unturned_Data/Managed/mods/Economy/");
                if (!File.Exists("Unturned_Data/Managed/mods/Economy/itemprices.ini"))
                {
                    IniFile _prices = new IniFile("Unturned_Data/Managed/mods/Economy/itemprices.ini");
                    _prices.IniWriteValue("Prices", "1", "1000");
                }
                string[] _itemprices = File.ReadAllLines("Unturned_Data/Managed/mods/Economy/itemprices.ini");
                for (int i = 0; i < _itemprices.Length; i++)
                {
                    string line = _itemprices[i];
                    string[] values = line.Split('=');
                    try
                    {
                        ItemPrices.Add(values[0], Convert.ToSingle(values[1]));
                    }
                    catch { }
                }
            }
            catch { return; }
        }
        public static void LoadVehiclePrices()
        {
            try
            {
                if (!Directory.Exists("Unturned_Data/Managed/mods/Economy/"))
                    Directory.CreateDirectory("Unturned_Data/Managed/mods/Economy/");
                if (!File.Exists("Unturned_Data/Managed/mods/Economy/vehicle_prices.ini"))
                {
                    IniFile _prices = new IniFile("Unturned_Data/Managed/mods/Economy/vehicle_prices.ini");
                    _prices.IniWriteValue("Prices", "apc_0", "1000");
                    _prices.IniWriteValue("Prices", "apc_1", "1000");
                    _prices.IniWriteValue("Prices", "car_0", "400");
                    _prices.IniWriteValue("Prices", "car_1", "900");
                    _prices.IniWriteValue("Prices", "fireTruck_0", "600");
                    _prices.IniWriteValue("Prices", "humvee_0", "900");
                    _prices.IniWriteValue("Prices", "humvee_1", "900");
                    _prices.IniWriteValue("Prices", "medic_0", "500");
                    _prices.IniWriteValue("Prices", "policeCar_0", "700");
                    _prices.IniWriteValue("Prices", "truck_0", "500");
                    _prices.IniWriteValue("Prices", "van_0", "500");
                }
                string[] _vehicleprices = File.ReadAllLines("Unturned_Data/Managed/mods/Economy/vehicle_prices.ini");
                for (int i = 0; i < _vehicleprices.Length; i++)
                {
                    string line = _vehicleprices[i];
                    string[] values = line.Split('=');
                    try
                    {
                        VehiclePrices.Add(values[0], Convert.ToSingle(values[1]));
                    }
                    catch { }
                }
            }
            catch { return; }
        }
        public static void CreateWallet(string steamid, float startBalance = 0)
        {
            wallets.Add(steamid, startBalance);
            SaveWallets();
            CalculateIndex();
        }
        public static float GetBalance(string steamid)
        {
            if (wallets.ContainsKey(steamid))
            {
                return wallets[steamid];
            }
            CreateWallet(steamid);
            SaveWallets();
            return 0;
        }
        public static void ChangeBalance(string steamid, float balance)
        {
            if (!wallets.ContainsKey(steamid))
            {
                CreateWallet(steamid);
            }
            wallets[steamid] = balance;
            SaveWallets();
        }
        public static int Pay(string sender, string receiver, float amount)
        {
            if (GetBalance(sender) < amount)
            {
                return -1;
            }
            if (!wallets.ContainsKey(receiver))
            {
                CreateWallet(receiver);
            }
            wallets[sender] -= amount;
            wallets[receiver] += amount;
            SaveWallets();
            return 0;
        }
        public static float VehicleCost(string vehicleId)
        {
            if (!VehiclePrices.ContainsKey(vehicleId))
                return -1;
            return VehiclePrices[vehicleId];
        }
        public static float Cost(string id)
        {
            if (!ItemPrices.ContainsKey(id))
                return -1;
            return ItemPrices[id];
        }
        public static int BuyVehicle(string VehicleId, string buyerSteamId)
        {
            float buyerBalance = GetBalance(buyerSteamId);
            float vehicleCost = VehicleCost(VehicleId);
            if (buyerBalance == 0)
                return -1;
            if (vehicleCost == -1)
                return -2;
            if (buyerBalance < vehicleCost)
                return -1;
            wallets[buyerSteamId] -= vehicleCost;
            ServerBalance += vehicleCost;
            SaveWallets();
            CalculateIndex();
            return 0;
        }
        public static int Buy(string Itemid, string buyerSteamId)
        {
            float buyerBalance = GetBalance(buyerSteamId);
            float itemCost = Cost(Itemid);
            if (buyerBalance == 0)
                return -1;
            if (itemCost == -1)
                return -2;
            if (buyerBalance < itemCost)
                return -1;
            wallets[buyerSteamId] -= itemCost;
            ServerBalance += itemCost;
            SaveWallets();
            CalculateIndex();
            return 0;
        }
        public static int Buy(string Itemid, string buyerSteamId, int amount)
        {
            float buyerBalance = GetBalance(buyerSteamId);
            float itemCost = Cost(Itemid);
            if (buyerBalance == 0)
                return -1;
            if (itemCost == -1)
                return -2;
            if (buyerBalance < (itemCost * amount))
                return -1;
            wallets[buyerSteamId] -= (itemCost * amount);
            ServerBalance += (itemCost * amount);
            SaveWallets();
            CalculateIndex();
            return 0;
        }
        public static int Sell(string Itemid, string sellerSteamId, int amount = 1)
        {
            float sellerBalance = GetBalance(sellerSteamId);
            float itemCost = Cost(Itemid);
            if (itemCost == -1)
                return -2;
            if (itemCost * amount > ServerBalance)
                return -3;
            wallets[sellerSteamId] += ((itemCost * amount) * SellIndex);
            ServerBalance -= ((itemCost * amount) * SellIndex);
            SaveWallets();
            CalculateIndex();
            return 0;
        }
    }
    public struct ReceivedData
    {
        public string senderName { get; }
        public float receivedAmount { get; }
        public ReceivedData(string senderName, float receivedAmount)
        {
            this.senderName = senderName;
            this.receivedAmount = receivedAmount;
        }
    }
}
