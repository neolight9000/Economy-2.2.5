using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using CommandHandler;
using Ini;
using UnityEngine;
using Economy;
using System.Linq;
using Random = UnityEngine.Random;

namespace Economy
{
	public class ShopMain : MonoBehaviour
	{
        private List<string> DeathPlayers = new List<string>();
        public void Start()
        {
            EconomyCore.LoadConfig();
            EconomyCore.LoadWallets();
            EconomyCore.SaveWallets();
            EconomyCore.LoadPrices();
            if (EconomyCore.SellVehicles)
            {
                EconomyCore.LoadVehiclePrices();
            }
            NetworkEvents.onPlayerConnected += EconomyCore._onPlayerConnected;
            Command pay = new Command(0, new CommandDelegate(Pay), new string[2]
            {
                "Pay", "pay"
            });
            CommandList.add(pay);
            Command balance = new Command(0, new CommandDelegate(Balance), new string[1]
            {
                "balance"
            });
            CommandList.add(new Command(0, new CommandDelegate(GetCurrentSellIndex), new string[] { "getindex", "currentsellindex", "sellindex" }));
            CommandList.add(balance);
            Command cost = new Command(0, new CommandDelegate(Cost), new string[4]
            {
                "cost","price","Cost", "price"
            });
            CommandList.add(cost);
            Command buy = new Command(0, new CommandDelegate(Buy), new string[2]
            {
                "buy", "Buy"
            });
            CommandList.add(buy);
            Command buyv = new Command(0, new CommandDelegate(BuyV), new string[4]
            {
                "buyV", "Buyv", "buyv", "BuyV"
            });
            CommandList.add(buyv);
            Command sell = new Command(0, new CommandDelegate(Sell), new string[1]
            {
                "sell"
            });
            CommandList.add(sell);
            Command SetBal = new Command(10, new CommandDelegate(SetBalance), new string[2] {"setbal","setbalance" });
            CommandList.add(SetBal);
            CommandList.add(new Command(0, new CommandDelegate(GetIndex), new string[] { "index", "getindex", "sellindex" }));
            new Thread(new ThreadStart(CheckDeathPlayers)).Start();
        }

        private void GetIndex(CommandArgs args)
        {
            Reference.Tell(args.sender.networkPlayer, $"Current Sell Index is {EconomyCore.SellIndex}x");
        }

        private void BuyV(CommandArgs args)
        {
            if (!EconomyCore.SellVehicles)
            {
                Reference.Tell(args.sender.networkPlayer, "Sorry, but this server not selling Vehicles.");
                return;
            }
            if(args.Parameters.Count < 1)
            {
                Reference.Tell(args.sender.networkPlayer, "You entered wrong parameters, try again");
                return;
            }
            string vehicleID = args.Parameters[0].Contains("_") ? args.Parameters[0] : GetCar.GetCarIdbyName(args.Parameters[0]);
            if (String.IsNullOrEmpty(vehicleID))
            {
                Reference.Tell(args.sender.networkPlayer, "You entered wrong vehicle name or id, try again.");
                return;
            }
            vehicleID = GetCar.ValidateVehicleId(vehicleID);
            if (String.IsNullOrEmpty(vehicleID))
            {
                Reference.Tell(args.sender.networkPlayer, "You entered wrong vehicle id, try again.");
                return;
            }
            int ServerAnswer = EconomyCore.BuyVehicle(vehicleID, args.sender.steamid);
            switch (ServerAnswer)
            {
                case -1:
                    Reference.Tell(args.sender.networkPlayer, "Not enough money to buy this item/s");
                    return;
                case -2:
                    Reference.Tell(args.sender.networkPlayer, "Sorry, but market aren't selling this vehicle");
                    return;
                case 0:
                    Reference.Tell(args.sender.networkPlayer, $"Successfully bought Vehicle with ID {vehicleID}");
                    break;
                default:
                    Reference.Tell(args.sender.networkPlayer, "Unknown error, try again.");
                    return;
            }
            Vector3 position = args.sender.position;
            Quaternion rotation = args.sender.rotation;
            Vector3 newPos = default(Vector3);
            ref Vector3 reference = ref newPos;
            reference = new Vector3(position.x + 5f,position.y + 50f,position.z);
            System.Threading.Timer timer = new System.Threading.Timer(delegate
            {
                SpawnVehicles.create(vehicleID, 100, 100, newPos, rotation * Quaternion.Euler(-90f, 0f, 0f), new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
                SpawnVehicles.save();
            }, null, 400, -1);

        }
        private void SetBalance(CommandArgs args)
        {
            BetterNetworkUser user = null;
            bool ParameterIsSteamId = true;
            float amount = 0;
            if (args.Parameters.Count < 1)
            {
                Reference.Tell(args.sender.networkPlayer, "Wrong parameters! Try again.");
                return;
            }
            if (args.Parameters.Count == 2)
            {
                float _Amount = Convert.ToSingle(args.Parameters[1]);
                if (_Amount < 0 || _Amount == 0)
                {
                    Reference.Tell(args.sender.networkPlayer, "You can't send amount less than zero or that equals to zero");
                    return;
                }
                amount = _Amount;
            }
            try { user = UserList.getUserFromName(args.Parameters[0]); }
            catch { }
            if (user != null)
            {
                EconomyCore.GetBalance(user.steamid);
                EconomyCore.ChangeBalance(user.steamid, amount);
                EconomyCore.SaveWallets();
            }
            else
            {
                for (int i = 0; i < args.Parameters[0].Length; i++)
                {
                    if (args.Parameters[0].Length != 17)
                    {
                        ParameterIsSteamId = false;
                    }
                    if (!Char.IsDigit(args.Parameters[0][i]))
                    {
                        ParameterIsSteamId = false;
                    }
                }
                if (ParameterIsSteamId)
                {
                    EconomyCore.GetBalance(args.Parameters[0]);
                    EconomyCore.ChangeBalance(args.Parameters[0], amount);
                    EconomyCore.SaveWallets();
                }
                else
                {
                    Reference.Tell(args.sender.networkPlayer, $"Entered wrong parameters, unknown player or steamid: \"{args.Parameters[0]}\" ");
                    return;
                }
            }

            
        }

        private void Pay(CommandArgs args)
        {
            BetterNetworkUser user = null;
            bool ParameterIsSteamId = true;
            float amount = 0;
            float senderBalance = EconomyCore.GetBalance(args.sender.steamid);
            if (args.Parameters.Count < 1)
            {
                Reference.Tell(args.sender.networkPlayer, "Wrong parameters! Try again.");
                return;
            }
            if (args.Parameters.Count == 2)
            {
                float _Amount = Convert.ToSingle(args.Parameters[1]);
                if (_Amount < 0 || _Amount == 0)
                {
                    Reference.Tell(args.sender.networkPlayer, "You can't send amount less than zero or that equals to zero");
                    return;
                }
                if(_Amount > senderBalance)
                {
                    Reference.Tell(args.sender.networkPlayer, "you cannot send amount, more than you have");
                    return;
                }
                amount = _Amount;
            }
            try { user = UserList.getUserFromName(args.Parameters[0]); }
            catch { }
            if (user != null)
            {
                EconomyCore.Pay(args.sender.steamid, user.steamid, amount);
                Reference.Tell(args.sender.networkPlayer, $"Successfully sent {amount} {EconomyCore.CoinSymbol} to {user.name}");
                Reference.Tell(user.networkPlayer, $"You successfully received {amount} {EconomyCore.CoinSymbol}, from {args.sender.name}");
                EconomyCore.SaveWallets();
            }
            else
            {
                for (int i = 0; i < args.Parameters[0].Length; i++)
                {
                    if (args.Parameters[0].Length != 17)
                    {
                        ParameterIsSteamId = false;
                    }
                    if (!Char.IsDigit(args.Parameters[0][i]))
                    {
                        ParameterIsSteamId = false;
                    }
                }
                if (ParameterIsSteamId)
                {
                    float receiveramount = EconomyCore.GetBalance(args.Parameters[0]);
                    EconomyCore.Pay(args.sender.steamid, args.Parameters[0], amount);
                    EconomyCore.SaveWallets();
                    EconomyCore.receiverAnnounce.Add(args.Parameters[0], new ReceivedData(args.sender.name, amount));
                    Reference.Tell(args.sender.networkPlayer, $"Successfully sent {amount} {EconomyCore.CoinSymbol} to {args.Parameters[0]}");
                }
                else
                {
                    Reference.Tell(args.sender.networkPlayer, $"Entered wrong parameters, unknown player or steamid: \"{args.Parameters[0]}\" ");
                    return;
                }
            }
        }
        private void Balance(CommandArgs args)
        {
            Reference.Tell(args.sender.networkPlayer, $"Your Balance is: {EconomyCore.GetBalance(args.sender.steamid)} {EconomyCore.CoinSymbol}");
        }
        private void Cost(CommandArgs args)
        {
            string itemid;
            bool ParameterIsDigits=true;
            for(int i = 0; i < args.Parameters[0].Length; i++)
            {
                if (!Char.IsDigit(args.Parameters[0][i]))
                {
                    ParameterIsDigits = false;
                }
            }
            if(ParameterIsDigits)
                itemid = args.Parameters[0];
            else
            {
                var _itemID = GetItem.GetItemIdFromName(args.Parameters[0]);
                itemid = _itemID;
                if (String.IsNullOrEmpty(itemid))
                {
                    Reference.Tell(args.sender.networkPlayer, $"You entered wrong parameters! item with name {args.Parameters[0]} does not exist. Enter id or valid name.");
                }
            }
            float price = EconomyCore.Cost(itemid);
            if(price == -1)
            {
                Reference.Tell(args.sender.networkPlayer, $"Sorry, market isn't selling item with ID {args.Parameters[0]}");
                return;
            }
            Reference.Tell(args.sender.networkPlayer, $"Cost of item with id {args.Parameters[0]} is {price} {EconomyCore.CoinSymbol}");
        }
        private void Buy(CommandArgs args)
        {
            int itemAmount = 1;
            string itemID = "0";
            int ServerAnswer;
            bool stackableItem = false;
            bool ParameterIsDigits = true;
            for (int i = 0; i < args.Parameters[0].Length; i++)
            {
                if (!Char.IsDigit(args.Parameters[0][i]))
                {
                    ParameterIsDigits = false;
                }
            }
            if (ParameterIsDigits)
                itemID = args.Parameters[0];
            else
            {
                var _itemID = GetItem.GetItemIdFromName(args.Parameters[0]);
                itemID = _itemID;
                if (String.IsNullOrEmpty(itemID))
                {
                    Reference.Tell(args.sender.networkPlayer, $"You entered wrong parameters! item with name {args.Parameters[0]} does not exist. Enter id or valid name.");
                }
            }
            if (args.Parameters.Count < 1)
            {
                Reference.Tell(args.sender.networkPlayer, "Wrong parameters! Try again.");
                return;
            }
            if(args.Parameters.Count == 2)
            {
                int _itemAmount = Convert.ToInt32(args.Parameters[1]);
                if(_itemAmount < 0 || _itemAmount == 0)
                {
                    Reference.Tell(args.sender.networkPlayer, "You can't set amount less than zero or that equals to zero");
                    return;
                }
                itemAmount = _itemAmount;
                ServerAnswer = EconomyCore.Buy(itemID, args.sender.steamid, itemAmount);
            }
            else
            {
                ServerAnswer = EconomyCore.Buy(itemID, args.sender.steamid);
            }
            switch (ServerAnswer)
            {
                case -1:
                    Reference.Tell(args.sender.networkPlayer, "Not enough money to buy this item/s");
                    return;
                case -2:
                    Reference.Tell(args.sender.networkPlayer, "Sorry, but market aren't selling this item");
                    return;
                case 0:
                    Reference.Tell(args.sender.networkPlayer, $"Successfully bought {itemAmount}x Items with ID {itemID}");
                    break;
                default:
                    Reference.Tell(args.sender.networkPlayer, "Unknown error, try again.");
                    return;
            }
            stackableItem = ItemStackable.getStackable(Convert.ToInt32(itemID));
            Inventory pinventory = args.sender.player.GetComponent<Inventory>();
            for (int i = 0; i < pinventory.clientItem_0.GetLength(0); i++)
            {
                for (int j = 0; j < pinventory.clientItem_0.GetLength(1); j++)
                {
                    try
                    {
                        ClientItem slot = pinventory.clientItem_0[i, j];
                        if(slot.Int32_0 == -1) // if slot is empty
                        {
                            if (stackableItem)
                            {
                                if (itemAmount > 0)
                                {
                                    args.sender.player.networkView.RPC("tellItemSlot", RPCMode.All, new object[] { i, j, Convert.ToInt32(itemID), itemAmount, ItemState.getState(Convert.ToInt32(itemID)) });
                                    args.sender.player.networkView.RPC("tellWeight", RPCMode.All, new object[] { ((ItemWeight.getWeight(int.Parse(itemID)) * itemAmount) + pinventory.int_3) });
                                    itemAmount = 0;
                                    pinventory.saveAllItems();
                                    break;
                                }
                            }
                            else
                            {
                                if (itemAmount > 0)
                                {
                                    args.sender.player.networkView.RPC("tellItemSlot", RPCMode.All, new object[] { i, j, Convert.ToInt32(itemID), 1, ItemState.getState(Convert.ToInt32(itemID)) });
                                    args.sender.player.networkView.RPC("tellWeight", RPCMode.All, new object[] { (ItemWeight.getWeight(int.Parse(itemID)) + pinventory.int_3) });
                                    pinventory.saveAllItems();
                                    itemAmount -=1;
                                }
                            }
                        }
              
                    }
                    catch { continue; }
                }
            }
            if (itemAmount != 0)
            {
                for (int i = 0; i < itemAmount; i++)
                {
                    SpawnItems.spawnItem(Convert.ToInt32(itemID), 1, args.sender.position);
                }
            }
            pinventory.saveAllItems();
        }
        private void GetCurrentSellIndex(CommandArgs args)
        {
            Reference.Tell(args.sender.networkPlayer, $"Current Sell Index is: {EconomyCore.SellIndex}x");
        }
        private void Sell(CommandArgs args)
        {
            int itemAmountInSeller = 0;
            int itemAmount = 1;
            string itemID = "0";
            int ServerAnswer = -2;
            int pastWeight = 0;
            bool ParameterIsDigits = true;
            float SellIndex = EconomyCore.SellIndex;
            for (int i = 0; i < args.Parameters[0].Length; i++)
            {
                if (!Char.IsDigit(args.Parameters[0][i]))
                {
                    ParameterIsDigits = false;
                }
            }
            if (ParameterIsDigits)
                itemID = args.Parameters[0];
            else
            {
                var _itemID = GetItem.GetItemIdFromName(args.Parameters[0]);
                itemID = _itemID;
                if (String.IsNullOrEmpty(itemID))
                {
                    Reference.Tell(args.sender.networkPlayer, $"You entered wrong parameters! item with name {args.Parameters[0]} does not exist. Enter id or valid name.");
                }
            }
            if (args.Parameters.Count < 1)
            {
                Reference.Tell(args.sender.networkPlayer, "Wrong parameters! Try again.");
                return;
            }
            if (args.Parameters.Count == 2)
            {
                int _itemAmount = Convert.ToInt32(args.Parameters[1]);
                if (_itemAmount < 0 || _itemAmount == 0)
                {
                    Reference.Tell(args.sender.networkPlayer, "You can't set amount less than zero or that equals to zero");
                    return;
                }
                itemAmount = _itemAmount;
            }
            if (!EconomyCore.ItemPrices.ContainsKey(itemID))
            {
                Reference.Tell(args.sender.networkPlayer, $"Sorry, but market aren't buying this item");
                return;
            }
            if((EconomyCore.Cost(itemID) * itemAmount) > EconomyCore.ServerBalance)
            {
                Reference.Tell(args.sender.networkPlayer, $"Sorry, the server does not have enough money to buy {itemAmount}x {itemID}");
                return;
            }
            Equipment.point2_0 = Point2.point2_1;
            Equipment.int_0 = -1;
            Equipment.bool_0 = false;
            Equipment.bool_1 = true;
            args.sender.player.networkView.RPC("equipServer", RPCMode.All, new object[] { -1, -1, Equipment.int_0 }); //dequipping item
            Inventory pinventory = args.sender.player.GetComponent<Inventory>();
            pastWeight = pinventory.int_3;
            for (int i = 0; i < pinventory.clientItem_0.GetLength(0); i++)
            {
                for (int j = 0; j < pinventory.clientItem_0.GetLength(1); j++)
                {
                    try
                    {
                        ClientItem slot = pinventory.clientItem_0[i, j];
                        if (slot.Int32_0 == int.Parse(itemID) && itemAmountInSeller < itemAmount) // if slot contains item
                        {
                            var slotItem = slot.Int32_1;
                            itemAmountInSeller+=slotItem;
                            if (itemAmountInSeller >= itemAmount)
                            {
                                for (int k = 0; k < itemAmount; k++)
                                {
                                    args.sender.player.networkView.RPC("askUseItem", RPCMode.All, new object[] { i, j});
                                    args.sender.player.networkView.RPC("equipServer", RPCMode.All, new object[] { i, j, -1});
                                }
                            }
                            else
                            {
                                for (int k = 0; k < itemAmountInSeller; k++)
                                {
                                    args.sender.player.networkView.RPC("askUseItem", RPCMode.All, new object[] { i, j});
                                    args.sender.player.networkView.RPC("equipServer", RPCMode.All, new object[] { i, j, -1 });
                                }
                            }


                        }

                    }
                    catch { continue; }
                }
            }
            if (itemAmountInSeller >= itemAmount)
            {
                ServerAnswer = EconomyCore.Sell(itemID, args.sender.steamid, itemAmount);
                    args.sender.player.networkView.RPC("tellWeight", RPCMode.All, new object[] { (pastWeight - (ItemWeight.getWeight(int.Parse(itemID)) * itemAmount)) });
            }
            else
            {
                EconomyCore.Sell(itemID, args.sender.steamid, (itemAmountInSeller));
                    args.sender.player.networkView.RPC("tellWeight", RPCMode.All, new object[] { (pastWeight - (ItemWeight.getWeight(int.Parse(itemID)) * itemAmountInSeller)) });
                ServerAnswer = -1;
            }
            pinventory.saveAllItems();
            switch (ServerAnswer)
            {
                case -1:
                    Reference.Tell(args.sender.networkPlayer, "Not enough items for sell");
                    return;
                case -2:
                    Reference.Tell(args.sender.networkPlayer, "Sorry, but market aren't buying this item");
                    return;
                case 0:
                    Reference.Tell(args.sender.networkPlayer, $"Successfully selled {itemAmount}x Items with ID {itemID}, you earned {EconomyCore.Cost(itemID)*itemAmount*SellIndex} {EconomyCore.CoinSymbol}");
                    break;
                default:
                    Reference.Tell(args.sender.networkPlayer, "Unknown error, try again.");
                    return;
            }

        }
        private void CheckDeathPlayers()
        {
            while (true)
            {
                foreach(var player in UserList.users)
                {
                    try
                    {
                        bool dead = player.model.GetComponent<Life>().bool_2;
                        if (dead && !DeathPlayers.Contains(player.steamid))
                        {
                            DeathPlayers.Add(player.steamid);
                            var Balance = EconomyCore.GetBalance(player.steamid);
                            if (Balance != 0)
                            {
                                float Penalty = Balance * EconomyCore.DeathIndex;
                                EconomyCore.ServerBalance += Penalty;
                                EconomyCore.ChangeBalance(player.steamid, Penalty);
                                EconomyCore.SaveWallets();
                                EconomyCore.CalculateIndex();
                            }
                        }
                        else if(!dead && DeathPlayers.Contains(player.steamid))
                        {
                            DeathPlayers.Remove(player.steamid);
                        }
                    }
                    catch { continue; }
                }
                Thread.Sleep(100);
            }
        }
    }
}
