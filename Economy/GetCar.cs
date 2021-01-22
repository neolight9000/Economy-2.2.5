using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Economy
{
    static class GetCar
    {
        public static string GetCarIdbyName(string carname)
        {
            switch (carname)
            {
                case "Police_Car":
                case "Police_car":
                case "police_car":
                case "police":
                case "Police":
                    return "policeCar_0";
                case "medic":
                case "ambulance":
                case "Ambulance":
                case "Medic":
                    return "medic_0";
                case "APC":
                case "apc":
                    return UnityEngine.Random.Range(1, 2) == 1 ? "apc_0" : "apc_1";
                case "car":
                case "Car":
                    return UnityEngine.Random.Range(1, 2) == 1 ? "car_0" : "car_1";
                case "Truck":
                case "truck":
                    return "truck_0";
                case "van":
                case "Van":
                    return "van_0";
                case "humvee":
                case "Humvee":
                case "Humve":
                case "humve":
                    return UnityEngine.Random.Range(1, 2) == 1 ? "humvee_0" : "humvee_1";
                case "Fire":
                case "Firetruck":
                case "fire":
                case "fireTruck":
                case "FireTruck":
                    return "fireTruck_0";
            }
            return String.Empty;
        }
        public static string ValidateVehicleId(string vehicleId)
        {
            switch (vehicleId)
            {
                case "policeCar_0":
                    return "policeCar_0";
                case "medic_0":
                    return "medic_0";
                case "apc_0":
                    return "apc_0";
                case "apc_1":
                    return "apc_1";
                case "car_0":
                    return "car_0";
                case "car_1":
                    return "car_1";
                case "truck_0":
                    return "truck_0";
                case "van_0":
                    return "van_0";
                case "humvee_0":
                    return "humvee_0";
                case "humvee_1":
                    return "humvee_1";
                case "fireTruck_0":
                    return "fireTruck_0";
            }
            return String.Empty;
        }
    }
}
