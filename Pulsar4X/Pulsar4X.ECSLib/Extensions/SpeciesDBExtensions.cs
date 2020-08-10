﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.ECSLib
{
    public static class SpeciesDBExtensions
    {
        public static bool CanSurviveGravityOn(this SpeciesDB species, Entity planet)
        {
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            double planetGravity = sysBody.Gravity;
            double maxGravity = species.MaximumGravityConstraint;
            double minGravity = species.MinimumGravityConstraint;

            if (planetGravity < minGravity || planetGravity > maxGravity)
                return false;
            return true;
        }

        public static double ColonyCost(this SpeciesDB species, Entity planet)
        {
            double cost = 1.0;

            cost = Math.Max(cost, species.ColonyPressureCost(planet));
            cost = Math.Max(cost, species.ColonyTemperatureCost(planet));
            cost = Math.Max(cost, species.ColonyGasCost(planet));
            cost = Math.Max(cost, species.ColonyToxicityCost(planet));

            if (!species.CanSurviveGravityOn(planet))
                return -1.0; // invalid - cannot create colony here

            return cost;
        }

        /// <summary>
        /// cost should increase with composition. there has to be a more efficent way of doing this too.
        /// </summary>
        /// <param name="planet"></param>
        /// <param name="species"></param>
        /// <returns></returns>
        public static double ColonyToxicityCost(this SpeciesDB species, Entity planet)
        {
            double cost = 1.0;
            double totalPressure = 0.0;
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                string symbol = kvp.Key.ChemicalSymbol;
                totalPressure += kvp.Value;

                if (kvp.Key.IsHighlyToxic)
                {
                    cost = Math.Max(cost, 3.0);
                }
                else if (kvp.Key.IsToxic)
                {
                    cost = Math.Max(cost, 2.0);
                }
            }

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                if (kvp.Key.IsHighlyToxicAtPercentage.HasValue)
                {
                    var percentageOfAtmosphere = Math.Round(kvp.Value / totalPressure * 100.0f, 4);
                    // if current % of atmosphere for this gas is over toxicity threshold
                    if (percentageOfAtmosphere >= kvp.Key.IsHighlyToxicAtPercentage.Value)
                    {
                        cost = Math.Max(cost, 3.0);
                    }
                }
                if (kvp.Key.IsToxicAtPercentage.HasValue)
                {
                    var percentageOfAtmosphere = Math.Round(kvp.Value / totalPressure * 100.0f, 4);
                    // if current % of atmosphere for this gas is over toxicity threshold
                    if (percentageOfAtmosphere >= kvp.Key.IsToxicAtPercentage.Value)
                    {
                        cost = Math.Max(cost, 2.0);
                    }
                }
            }

            return cost;
        }

        public static double ColonyPressureCost(this SpeciesDB species, Entity planet)
        {
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            if (atmosphere == null)
            {
                // No atmosphere on the planet, return 1.0?
                // @todo - some other rule for no atmosphere planets?
                return 1.0;
            }

            var totalPressure = atmosphere.GetAtmosphericPressure();
            if (totalPressure > species.MaximumPressureConstraint)
            {
                // AuroraWiki: If the pressure is too high, the colony cost will be equal to the Atmospheric Pressure 
                //             divided by the species maximum pressure with a minimum of 2.0
                
                return Math.Round(Math.Max(totalPressure / species.MaximumPressureConstraint, 2.0), 6);
            }

            return 1.0;
        }

        public static double ColonyTemperatureCost(this SpeciesDB species, Entity planet)
        {
            // AuroraWiki : The colony cost for a temperature outside the range is Temperature Difference / Temperature Deviation. 
            //              So if the deviation was 22 and the temperature was 48 degrees below the minimum, the colony cost would be 48/22 = 2.18
            SystemBodyInfoDB sysBody = planet.GetDataBlob<SystemBodyInfoDB>();
            double cost;
            double idealTemp = species.BaseTemperature;
            double planetTemp = sysBody.BaseTemperature;  // @todo: find correct temperature after terraforming
            double tempRange = species.TemperatureToleranceRange;

            //More Math (the | | signs are for Absolute Value in case you forgot)
            //TempColCost = | Ideal Temp - Current Temp | / TRU (temps in Kelvin)
            // Converting to Kelvin.  It probably doesn't matter, but just in case
            cost = Math.Abs((idealTemp + 273.15) - (planetTemp + 273.15)) / tempRange;

            return cost;
        }

        // Returns cost based on amount of breathable gas in atmosphere
        public static double ColonyGasCost(this SpeciesDB species, Entity planet)
        {
            // @todo: update to check species for its breathable gas

            double cost = 1.0;
            float O2Pressure = 0.0f;
            float totalPressure = 0.0f;
            AtmosphereDB atmosphere = planet.GetDataBlob<AtmosphereDB>();

            if (atmosphere == null)
            {
                // No atmosphere on the planet, return 2.0?
                // @todo - some other rule for no atmosphere planets?
                return 2.0;
            }

            Dictionary<AtmosphericGasSD, float> atmosphereComp = atmosphere.Composition;

            foreach (KeyValuePair<AtmosphericGasSD, float> kvp in atmosphereComp)
            {
                string symbol = kvp.Key.ChemicalSymbol;
                totalPressure += kvp.Value;
                if (symbol == "O2")
                    O2Pressure = kvp.Value;
            }

            //if (totalPressure >= 4.0f && O2Pressure <= 0.31f)
            //    cost = cost; // created for the break point

            if (totalPressure == 0.0f) // No atmosphere, obviously not breathable
                return 2.0;

            if (O2Pressure < 0.1f || O2Pressure > 0.3f)  // wrong amount of oxygen
                return 2.0;

            if (O2Pressure / totalPressure > 0.3f) // Oxygen cannot be more than 30% of atmosphere to be breathable
                return 2.0;

            return cost;
        }
    }
}