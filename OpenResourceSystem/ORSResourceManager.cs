﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem 
{
    public class ORSResourceManager 
    {
        public const string STOCK_RESOURCE_ELECTRICCHARGE = "ElectricCharge";
        public const string FNRESOURCE_MEGAJOULES = "Megajoules";
        public const string FNRESOURCE_CHARGED_PARTICLES = "ChargedParticles";
        public const string FNRESOURCE_THERMALPOWER = "ThermalPower";
		public const string FNRESOURCE_WASTEHEAT = "WasteHeat";
		public const int FNRESOURCE_FLOWTYPE_SMALLEST_FIRST = 0;
		public const int FNRESOURCE_FLOWTYPE_EVEN = 1;
		protected const double passive_temp_p4 = 2947.295521;
               
        protected Vessel my_vessel;
        protected Part my_part;
        protected PartModule my_partmodule;
        protected Dictionary<ORSResourceSuppliable, double> power_draws;

        protected Dictionary<ORSResourceSupplier, double> power_supplies;
        //protected Dictionary<ORSResourceSupplier, double> power_supplies_stable;

		//List<PartResource> partresources;
        protected String resource_name;
        //protected Dictionary<MegajouleSuppliable, float> power_returned;
        protected double currentPowerSupply = 0;
		protected double stable_supply = 0;
		protected double stored_stable_supply = 0;
        protected double stored_resource_demand = 0;
        protected double stored_current_hp_demand;
		protected double current_resource_demand = 0;
		protected double high_priority_resource_demand = 0;
		protected double charge_resource_demand = 0;
        protected double stored_supply = 0;
        protected double stored_charge_demand = 0;
		protected int flow_type = 0;
        protected List<KeyValuePair<ORSResourceSuppliable, double>> power_draw_list_archive;
        protected bool render_window = false;
        protected Rect windowPosition = new Rect(200, 200, 300, 100);
        protected int windowID = 36549835;
		protected double resource_bar_ratio = 0;
        protected GUIStyle bold_label;
        protected GUIStyle green_label;
        protected GUIStyle red_label;
        protected GUIStyle right_align;
        protected double internl_power_extract = 0;

        public ORSResourceManager(PartModule pm,String resource_name) 
        {
            my_vessel = pm.vessel;
            my_part = pm.part;
            my_partmodule = pm;

            power_draws = new Dictionary<ORSResourceSuppliable,double>();
            power_supplies = new Dictionary<ORSResourceSupplier, double>();

            this.resource_name = resource_name;

			if (resource_name == FNRESOURCE_WASTEHEAT || resource_name == FNRESOURCE_THERMALPOWER) 
				flow_type = FNRESOURCE_FLOWTYPE_EVEN;
			else 
				flow_type = FNRESOURCE_FLOWTYPE_SMALLEST_FIRST;
        }

        public void powerDraw(ORSResourceSuppliable pm, double power_draw) 
        {
            if (power_draws.ContainsKey(pm)) 
            {
                power_draw = power_draw / TimeWarp.fixedDeltaTime + power_draws[pm];
                power_draws[pm] = power_draw;
            }
            else 
            {
                power_draws.Add(pm, power_draw / TimeWarp.fixedDeltaTime);
            }
        }

        public float powerSupply(ORSResourceSupplier pm, float power) 
        {
            return (float) powerSupply (pm,(double)power);
        }

        public double powerSupply(ORSResourceSupplier pm, double power) 
        {
            currentPowerSupply += (power / TimeWarp.fixedDeltaTime);
			stable_supply += (power / TimeWarp.fixedDeltaTime);

            if (power_supplies.ContainsKey(pm)) 
                power_supplies[pm] += (power / TimeWarp.fixedDeltaTime);
            else 
                power_supplies.Add(pm, (power / TimeWarp.fixedDeltaTime));
            
            return power;
        }

        public float powerSupplyFixedMax(ORSResourceSupplier pm, float power, float maxpower) 
        {
			return (float) powerSupplyFixedMax (pm, (double)power,(double)maxpower);
		}

        public double powerSupplyFixedMax(ORSResourceSupplier pm, double power, double maxpower) 
        {
			currentPowerSupply += (power / TimeWarp.fixedDeltaTime);
			stable_supply += (maxpower / TimeWarp.fixedDeltaTime);

            if (power_supplies.ContainsKey(pm)) 
                power_supplies[pm] += (power / TimeWarp.fixedDeltaTime);
            else 
                power_supplies.Add(pm, (power / TimeWarp.fixedDeltaTime));
			return power;
		}

        public float managedPowerSupply(ORSResourceSupplier pm, float power) 
        {
			return managedPowerSupplyWithMinimumRatio (pm, power, 0);
		}

        public double managedPowerSupply(ORSResourceSupplier pm, double power) 
        {
			return managedPowerSupplyWithMinimumRatio (pm, power, 0);
		}

        public double getResourceAvailability()
        {
            return my_part.GetConnectedResources(resource_name).ToList()
                .Sum(partresource => partresource.amount); ;
        }

		public double getSpareResourceCapacity() 
        {
            return my_part.GetConnectedResources(resource_name).ToList()
                .Sum(partresource => partresource.maxAmount - partresource.amount); ;
		}

        public double getTotalResourceCapacity()
        {
            return my_part.GetConnectedResources(resource_name).ToList()
                .Sum(partresource => partresource.maxAmount);
		}

        public float managedPowerSupplyWithMinimumRatio(ORSResourceSupplier pm, float power, float rat_min) 
        {
            return (float) managedPowerSupplyWithMinimumRatio(pm, (double)power, (double)rat_min);
		}

        public double managedPowerSupplyWithMinimumRatio(ORSResourceSupplier pm, double power, double rat_min) 
        {
			var maximum_available_power_per_second = power / TimeWarp.fixedDeltaTime;
            var minimum_power_per_second = maximum_available_power_per_second * rat_min;
            var required_power_per_second = Math.Max(GetRequiredResourceDemand(), minimum_power_per_second);
            var managed_supply_per_second = Math.Min(maximum_available_power_per_second, required_power_per_second);

			currentPowerSupply += managed_supply_per_second;
			stable_supply += maximum_available_power_per_second;

            if (power_supplies.ContainsKey(pm))
                power_supplies[pm] += maximum_available_power_per_second;
            else
                power_supplies.Add(pm, maximum_available_power_per_second);

			return managed_supply_per_second * TimeWarp.fixedDeltaTime;
		}

        public float getStableResourceSupply() 
        {
            return (float) stored_stable_supply;
        }

        public float getResourceSupply() 
        {
            return (float)stored_supply;
        }

        public double getDemandSupply()
        {
            return stored_supply - stored_resource_demand;
        }

        public double getDemandStableSupply()
        {
            return stored_resource_demand / stored_stable_supply;
        }

        public float getResourceDemand() 
        {
            return (float)stored_resource_demand;
        }

		public float getCurrentResourceDemand() 
        {
			return (float) current_resource_demand;
		}

        public float getCurrentHighPriorityResourceDemand() 
        {
            return (float)stored_current_hp_demand;
		}

		public float getCurrentUnfilledResourceDemand() 
        {
			return (float)(current_resource_demand - currentPowerSupply);
		}

        public float GetRequiredResourceDemand()
        {
            return getCurrentUnfilledResourceDemand() + (float)getSpareResourceCapacity() / TimeWarp.fixedDeltaTime;
        }

        public float GetPowerSupply()
        {
            return (float)currentPowerSupply;
        }

        public float GetCurrentRresourceDemand()
        {
            return (float)current_resource_demand;
        }

		public double getResourceBarRatio() 
        {
			return resource_bar_ratio;
		}

        public Vessel getVessel() 
        {
            return my_vessel;
        }

		public void updatePartModule(PartModule pm) 
        {
			my_vessel = pm.vessel;
			my_part = pm.part;
			my_partmodule = pm;
		}

		public PartModule getPartModule() 
        {
			return my_partmodule;
		}

        public void update() 
        {
            stored_supply = currentPowerSupply;
			stored_stable_supply = stable_supply;
            stored_resource_demand = current_resource_demand;
			double stored_current_demand = current_resource_demand;
			stored_current_hp_demand = high_priority_resource_demand;
			double stored_current_charge_demand = charge_resource_demand;
            stored_charge_demand = charge_resource_demand;

			current_resource_demand = 0;
			high_priority_resource_demand = 0;
			charge_resource_demand = 0;

			//Debug.Log ("Early:" + powersupply);

            //stored power
            List<PartResource> partresources = my_part.GetConnectedResources(resource_name).ToList();
            double currentmegajoules = 0;
			double maxmegajoules = 0;

            foreach (PartResource partresource in partresources) 
            {
                currentmegajoules += partresource.amount;
				maxmegajoules += partresource.maxAmount;
            }

			if (maxmegajoules > 0) 
				resource_bar_ratio = currentmegajoules / maxmegajoules;
            else 
				resource_bar_ratio = 0;

			double missingmegajoules = maxmegajoules - currentmegajoules;
            currentPowerSupply += currentmegajoules;
			//Debug.Log ("Current:" + currentmegajoules);

			double demand_supply_ratio = 0;
			double high_priority_demand_supply_ratio = 0;

			if (high_priority_resource_demand > 0) 
				high_priority_demand_supply_ratio = Math.Min ((currentPowerSupply-stored_current_charge_demand) / stored_current_hp_demand, 1.0);
			else 
				high_priority_demand_supply_ratio = 1.0;
			

			if (stored_current_demand > 0) 
				demand_supply_ratio = Math.Min ((currentPowerSupply-stored_current_charge_demand-stored_current_hp_demand) / stored_current_demand, 1.0);
			else 
				demand_supply_ratio = 1.0;

			//Prioritise supplying stock ElectricCharge resource
			if (String.Equals(this.resource_name, ORSResourceManager.FNRESOURCE_MEGAJOULES) && stored_stable_supply > 0) 
            {
				List<PartResource> electric_charge_resources = my_part.GetConnectedResources ("ElectricCharge").ToList(); 
				double stock_electric_charge_needed = 0;
				foreach (PartResource partresource in electric_charge_resources) {
					stock_electric_charge_needed += partresource.maxAmount - partresource.amount;
				}
				double power_supplied = Math.Min(currentPowerSupply*1000*TimeWarp.fixedDeltaTime, stock_electric_charge_needed);
                if (stock_electric_charge_needed > 0) {
                    current_resource_demand += stock_electric_charge_needed / 1000.0 / TimeWarp.fixedDeltaTime;
                    charge_resource_demand += stock_electric_charge_needed / 1000.0 / TimeWarp.fixedDeltaTime;
                }
				if (power_supplied > 0) {
                    currentPowerSupply += my_part.RequestResource("ElectricCharge", -power_supplied) / 1000 / TimeWarp.fixedDeltaTime;
				}
			}

			//sort by power draw
			//var power_draw_items = from pair in power_draws orderby pair.Value ascending select pair;
			List<KeyValuePair<ORSResourceSuppliable, double>> power_draw_items = power_draws.ToList();

            power_draw_items.Sort(delegate(KeyValuePair<ORSResourceSuppliable, double> firstPair, KeyValuePair<ORSResourceSuppliable, double> nextPair) { return firstPair.Value.CompareTo(nextPair.Value); });
            power_draw_list_archive = power_draw_items.ToList();
            power_draw_list_archive.Reverse();
            
            // check engines
            foreach (KeyValuePair<ORSResourceSuppliable, double> power_kvp in power_draw_items) 
            {
                ORSResourceSuppliable ms = power_kvp.Key;

                if (ms.getPowerPriority() == 1) 
                {
                    double power = power_kvp.Value;
					current_resource_demand += power;
					high_priority_resource_demand += power;

					if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) 
						power = power * high_priority_demand_supply_ratio;
					
                    double power_supplied = Math.Max(Math.Min(currentPowerSupply, power),0.0);
					//Debug.Log (power + ", " + powersupply + "::: " + power_supplied);
                    currentPowerSupply -= power_supplied;
					//notify of supply
                    ms.receiveFNResource(power_supplied, this.resource_name);
                }

            }
            // check others
            foreach (KeyValuePair<ORSResourceSuppliable, double> power_kvp in power_draw_items) 
            {
                ORSResourceSuppliable ms = power_kvp.Key;
                
                if (ms.getPowerPriority() == 2) 
                {
                    double power = power_kvp.Value;
					current_resource_demand += power;

					if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) 
						power = power * demand_supply_ratio;
					
					double power_supplied = Math.Max(Math.Min(currentPowerSupply, power),0.0);
                    currentPowerSupply -= power_supplied;

					//notify of supply
					ms.receiveFNResource(power_supplied, this.resource_name);
                }

            }
			// check radiators
            foreach (KeyValuePair<ORSResourceSuppliable, double> power_kvp in power_draw_items) 
            {
				ORSResourceSuppliable ms = power_kvp.Key;
				if (ms.getPowerPriority() == 3) 
                {
					double power = power_kvp.Value;
					current_resource_demand += power;
					if (flow_type == FNRESOURCE_FLOWTYPE_EVEN) 
						power = power * demand_supply_ratio;

					double power_supplied = Math.Max(Math.Min(currentPowerSupply, power), 0.0);
					currentPowerSupply -= power_supplied;

					//notify of supply
                    ms.receiveFNResource(power_supplied, this.resource_name);
				}
			}


            currentPowerSupply -= Math.Max(currentmegajoules, 0.0);

			internl_power_extract = -currentPowerSupply * TimeWarp.fixedDeltaTime;

            pluginSpecificImpl();

            if (internl_power_extract > 0) 
                internl_power_extract = Math.Min(internl_power_extract, currentmegajoules);
            else
                internl_power_extract = Math.Max(internl_power_extract, -missingmegajoules);

            //my_part.RequestResource(this.resource_name, internl_power_extract);
            ORSHelper.fixedRequestResource(my_part, this.resource_name, internl_power_extract);
            currentPowerSupply = 0;
			stable_supply = 0;

            power_supplies.Clear();
            power_draws.Clear();
        }

        protected virtual void pluginSpecificImpl() 
        {

        }

        public void showWindow() 
        {
            render_window = true;
        }

        public void hideWindow() 
        {
            render_window = false;
        }

        public void OnGUI() 
        {
            if (my_vessel == FlightGlobals.ActiveVessel && render_window) 
            {
                string title = resource_name + " Power Management Display";
                windowPosition = GUILayout.Window(windowID, windowPosition, doWindow, title);
            }
        }

        protected virtual void doWindow(int windowID) {
           
        }

        protected string getPowerFormatString(double power) 
        {
            if (Math.Abs(power) >= 1000) 
            {
                if (Math.Abs(power) > 20000) 
                    return (power / 1000).ToString("0.0") + " GW";
                else 
                    return (power / 1000).ToString("0.00") + " GW";
            } 
            else 
            {
                if (Math.Abs(power) > 20) 
                    return power.ToString("0.0") + " MW";
                else 
                {
                    if (Math.Abs(power) >= 1) 
                        return power.ToString("0.00") + " MW";
                    
                    else 
                        return (power * 1000).ToString("0.0") + " KW";
                }
            }
        }
    }
}
