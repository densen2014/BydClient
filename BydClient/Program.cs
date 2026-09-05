using BydClient.Config;
using BydClient.Exceptions;
using BydClient.Models;
using Microsoft.Extensions.Configuration;

namespace BydClient;

record Appsettings(string username, string password, string baseUrl, string countryCode, string language);
internal class Program
{
    static async Task Main()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        Appsettings defaults = new("test@example.com", "password123", "https://dilinkappoversea-eu.byd.auto", "NL", "en");
        Appsettings fileSettings = config.GetSection("BYD").Get<Appsettings>() ?? defaults;
        Appsettings appsettings = new(
            Environment.GetEnvironmentVariable("BYD_USERNAME") ?? fileSettings.username,
            Environment.GetEnvironmentVariable("BYD_PASSWORD") ?? fileSettings.password,
            Environment.GetEnvironmentVariable("BYD_BASE_URL") ?? fileSettings.baseUrl,
            Environment.GetEnvironmentVariable("BYD_COUNTRY_CODE") ?? fileSettings.countryCode,
            Environment.GetEnvironmentVariable("BYD_LANGUAGE") ?? fileSettings.language);

        BydConfig bydConfig = new(appsettings.username, appsettings.password, appsettings.baseUrl, appsettings.countryCode, appsettings.language);
        using Client client = new(bydConfig);

        try
        {
            Console.WriteLine("Logging in...");
            await client.LoginAsync();
            Console.WriteLine("Login successful!\n");

            Console.WriteLine("Fetching vehicles...");
            IReadOnlyList<Vehicle> vehicles = await client.GetVehiclesAsync();

            foreach(Vehicle vehicle in vehicles)
            {
                Console.WriteLine($"=== Vehicle: {vehicle.ModelName} ({vehicle.Vin}) ===");
                Console.WriteLine($"Brand: {vehicle.BrandName}");
                Console.WriteLine($"Plate: {vehicle.AutoPlate}\n");

                // Realtime Data
                Console.WriteLine("--- Realtime Data ---");
                VehicleRealtimeData realtime = await client.GetVehicleRealtimeAsync(vehicle.Vin);
                Console.WriteLine($"Online: {realtime.OnlineState}");
                Console.WriteLine($"Vehicle state: {realtime.VehicleState}");
                Console.WriteLine($"Battery: {realtime.ElecPercent}%");
                Console.WriteLine($"Range: {realtime.EnduranceMileageV2} {realtime.EnduranceMileageV2Unit}");
                Console.WriteLine($"Total mileage: {realtime.TotalMileageV2} {realtime.TotalMileageV2Unit}");
                Console.WriteLine($"Charge state: {realtime.ChargeState}");
                Console.WriteLine($"Locked: {(realtime.IsLocked().HasValue ? (realtime.IsLocked()!.Value ? "yes" : "no") : "unknown")}");
                Console.WriteLine($"Doors open: {(realtime.IsAnyDoorOpen() ? "yes" : "no")}");
                Console.WriteLine($"Windows open: {(realtime.IsAnyWindowOpen() ? "yes" : "no")}");
                Console.WriteLine($"Tire pressure unit: {realtime.TirePressUnit}");

                if(realtime.IsInteriorTempAvailable())
                    Console.WriteLine($"Interior temp: {realtime.TempInCar}°C");

                Console.WriteLine();

                // HVAC
                Console.WriteLine("--- HVAC Status ---");
                try
                {
                    HvacStatus hvac = await client.GetHvacStatusAsync(vehicle.Vin);
                    Console.WriteLine($"HVAC status: {hvac.Status}");
                    Console.WriteLine($"AC mode: {hvac.AirConditioningMode}");
                    Console.WriteLine($"Wind mode: {hvac.WindMode}");
                    Console.WriteLine($"Driver seat heat: {hvac.MainSeatHeatState}");
                    Console.WriteLine($"Steering wheel heat: {hvac.SteeringWheelHeatState}");
                    if(hvac.TempInCar.HasValue)
                        Console.WriteLine($"Interior temp: {hvac.TempInCar}°C");
                }
                catch(BydException ex)
                {
                    Console.WriteLine($"HVAC error: {ex.Message}");
                }
                Console.WriteLine();

                // GPS
                Console.WriteLine("--- GPS Info ---");
                try
                {
                    GpsInfo gps = await client.GetGpsInfoAsync(vehicle.Vin);
                    if(gps.Latitude.HasValue && gps.Longitude.HasValue)
                    {
                        Console.WriteLine($"Location: {gps.Latitude}, {gps.Longitude}");
                        Console.WriteLine($"Speed: {gps.Speed} km/h");
                    }
                    else
                    {
                        Console.WriteLine("GPS: no data available");
                    }
                }
                catch(BydException ex)
                {
                    Console.WriteLine($"GPS error: {ex.Message}");
                }
                Console.WriteLine();

                // Charging
                Console.WriteLine("--- Charging Status ---");
                try
                {
                    ChargingStatus charging = await client.GetChargingStatusAsync(vehicle.Vin);
                    Console.WriteLine($"Charging state: {charging.ChargingState}");
                }
                catch(BydException ex)
                {
                    Console.WriteLine($"Charging error: {ex.Message}");
                }
                Console.WriteLine();

                // Energy
                Console.WriteLine("--- Energy Consumption ---");
                try
                {
                    EnergyConsumption energy = await client.GetEnergyConsumptionAsync(vehicle.Vin);
                    // You can print energy details here
                    Console.WriteLine($"Total Mileage: {energy.TotalMileage}");
                    Console.WriteLine($"Total Energy: {energy.TotalEnergy}");
                    Console.WriteLine($"Recent Average Energy: {energy.RecentAverageEnergy}");
                    Console.WriteLine($"Recent 50 km Energy: {energy.Recent50kmEnergy}");
                    Console.WriteLine($"Driving Energy: {energy.DrivingEnergy}");
                    Console.WriteLine($"Charging Energy: {energy.ChargingEnergy}");
                    Console.WriteLine($"Electric Mileage: {energy.ElectricMileage}");
                    Console.WriteLine($"Fuel Mileage: {energy.FuelMileage}");
                    Console.WriteLine($"Total Mileage Of Electric: {energy.TotalMileageOfElectric}");
                    Console.WriteLine($"Total Mileage Of Fuel : {energy.TotalMileageOfFuel}");
                    Console.WriteLine($"Total Energy Of Electric: {energy.TotalEnergyOfElectric}");
                    Console.WriteLine($"Total Energy Of Fuel: {energy.TotalEnergyOfFuel}");
                    Console.WriteLine($"Co2 Emission: {energy.Co2Emission}");
                    Console.WriteLine($"Co2 Saved: {energy.Co2Saved}");
                    Console.WriteLine($"Start Time: {energy.StartTime}");
                    Console.WriteLine($"End Time: {energy.EndTime}");
                }
                catch(BydException ex)
                {
                    Console.WriteLine($"Energy error: {ex.Message}");
                }
                Console.WriteLine();
            }
        }
        catch(BydException ex)
        {
            Console.WriteLine($"BYD API Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"General Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        // Controls command
        // TO DO
    }
}

