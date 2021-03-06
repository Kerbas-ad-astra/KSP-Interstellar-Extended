﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using TweakScale;

namespace InterstellarFuelSwitch
{
    public class IFSresource
    {
        public int ID;
        public string name;
        public double currentSupply;
        public double amount;
        public double maxAmount;
        public double boiloffTemp;
        public double density;
        public double unitCost;
        public double latendHeatVaporation;
        public double specificHeatCapacity;


        public IFSresource(string name)
        {
            ID = name.GetHashCode();
            this.name = name;
            PartResourceDefinition resourceDefinition = PartResourceLibrary.Instance.GetDefinition(name);
            if (resourceDefinition != null)
            {
                this.density = resourceDefinition.density;
                this.unitCost = resourceDefinition.unitCost;
                this.specificHeatCapacity = resourceDefinition.specificHeatCapacity;
            }
        }

        public double FullMass { get { return maxAmount * density; } }
    }

    public class IFSmodularTank
    {
        public string GuiName = String.Empty;
        public string SwitchName = String.Empty;
        public string techReq;
        public bool hasTech;
        public double tankCost;
        public double tankMass;
        public double resourceMassDivider;

        public List<IFSresource> Resources = new List<IFSresource>();

        public double FullMass { get { return Resources.Sum(m => m.FullMass); } }
    }

    public class InterstellarFuelSwitch : PartModule, IRescalable<InterstellarFuelSwitch>, IPartCostModifier, IPartMassModifier
    {
        // Persistants
        [KSPField(isPersistant = true)]
        [UI_ChooseOption(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.Editor, suppressEditorShipModified = true)]
        public int selectedTankSetup = -1;
        [KSPField(isPersistant = true)]
        public int inFlightTankSetup = -1;

        [KSPField(isPersistant = true)]
        public string configuredAmounts = "";
        [KSPField(isPersistant = true)]
        public string selectedTankSetupTxt;
        //[KSPField(isPersistant = true)]
        //public bool gameLoaded = false;
        [KSPField(isPersistant = true)]
        public bool configLoaded = false;

        // Config properties
        [KSPField]
        public string resourceGui = "";
        [KSPField]
        public string tankSwitchNames = "";
        [KSPField]
        public string nextTankSetupText = "Next tank setup";
        [KSPField]
        public string previousTankSetupText = "Previous tank setup";
        [KSPField]
        public string switcherDescription = "Tank";
        [KSPField]
        public string resourceNames = "ElectricCharge;LiquidFuel,Oxidizer;MonoPropellant";
        [KSPField]
        public string resourceAmounts = "";
        [KSPField]
        public string resourceRatios = "";
        [KSPField]
        public string initialResourceAmounts = "";

        [KSPField(guiActiveEditor = false)]
        public float basePartMass = 0;
        [KSPField(guiActiveEditor = false)]
        public float baseResourceMassDivider = 0;
        [KSPField(guiActiveEditor = false)]
        public string tankResourceMassDivider = "";

        [KSPField]
        public float initialPrefabAmount = 0;
        [KSPField]
        public string tankMass = "";
        [KSPField]
        public string tankTechReq = "";
        [KSPField]
        public string tankCost = "";
        [KSPField]
        public string boilOffTemp = "";
        [KSPField]
        public string latendHeatVaporation = "";
        [KSPField]
        public bool displayCurrentBoilOffTemp = false;
        [KSPField]
        public bool displayCurrentTankCost = false;
        [KSPField]
        public bool hasSwitchChooseOption = true;
        [KSPField]
        public bool hasGUI = false;
        [KSPField]
        public bool boiloffActive = false;
        [KSPField(guiActive = false)]
        public bool availableInFlight = false;
        [KSPField]
        public bool availableInEditor = true;
        [KSPField(guiActive = false)]
        public bool isEmpty = false;

        [KSPField]
        public string inEditorSwitchingTechReq;
        [KSPField]
        public string inFlightSwitchingTechReq;
        [KSPField]
        public bool useTextureSwitchModule = false;
        //[KSPField(guiActiveEditor = false)]
        //public float initiaAmount;

        //[KSPField]
        //public bool showTemperature = false;
        [KSPField]
        public bool showTankName = true;
        [KSPField]
        public bool showInfo = true; // if false, does not feed info to the part list pop up info menu
        [KSPField]
        public string resourcesFormat = "0.0000";

        //[KSPField]
        //public string resourcesToIgnore = ""; // obsolete
        //[KSPField]
        //public float heatConvectiveConstant = 0.01f;
        //[KSPField]
        //public float heatConductivity = 0.01f;

        //dummyvalues
        [KSPField]
        public float volumeMultiplier;
        [KSPField]
        public float massMultiplier;

        // Gui
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Temp")]
        public string partTemperatureStr = String.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Specific Heat")]
        public string specificHeatStr = String.Empty;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Heat Absorbed")]
        public string boiloffEnergy = String.Empty;

        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Tank")]
        public string tankGuiName = String.Empty;
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Added cost")]
        public float addedCost = 0;
        //[KSPField(guiActive = false, guiActiveEditor = false, guiName = "Boiloff Temp")]
        //public string currentBoiloffTempStr = "";
        [KSPField(guiActive = false, guiActiveEditor = true, guiName = "Mass Ratio")]
        public string massRatioStr = "";


        // Debug
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Dry mass", guiUnits = " t", guiFormat = "F4")]
        public double dryMass = 0;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Part mass", guiUnits = " t", guiFormat = "F4")]
        public double currentPartMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Initial mass", guiUnits = " t", guiFormat = "F4")]
        public double initialMass;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Delta mass", guiUnits = " t", guiFormat = "F4")]
        public double moduleMassDelta;
        [KSPField(guiActive = false, guiActiveEditor = false, guiName = "Default mass", guiUnits = " t", guiFormat = "F4")]
        public float defaultMass;

        [KSPField(isPersistant = true)]
        public float storedVolumeMultiplier = 1;
        [KSPField(isPersistant = true)]
        public float storedMassMultiplier = 1;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Wet mass", guiUnits = " t", guiFormat = "F4")]
        public double wetMass;
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr0 = "";
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr1 = "";
        [KSPField(guiActiveEditor = false, guiActive = false)]
        public string resourceAmountStr2 = "";
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Total mass", guiUnits = " t", guiFormat = "F4")]
        public double totalMass;

        // Obsolete
        //[KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Volume Multiplier")]
        //public float volumeMultiplier = 1;
        //[KSPField(isPersistant = false, guiActiveEditor = false, guiName = "Mass Multiplier")]
        //public float massMultiplier = 1;

        [KSPField(guiActiveEditor = false, guiName = "Volume Exponent")]
        public float volumeExponent = 3;
        [KSPField(guiActiveEditor = false, guiName = "Mass Exponent")]
        public float massExponent = 3;
        [KSPField(isPersistant = true)]
        public bool traceBoiloff;

        private InterstellarTextureSwitch2 textureSwitch;
        private List<IFSmodularTank> _modularTankList = new List<IFSmodularTank>();
        private HashSet<string> activeResourceList = new HashSet<string>();

        private List<double> weightList;
        private List<double> tankCostList;
        private List<double> tankResourceMassDividerList;
        private bool initialized = false;

        private double initializePartTemperature = -1;

        private PartResource _partResource0;
        private PartResource _partResource1;
        private PartResource _partResource2;

        private PartResourceDefinition _partRresourceDefinition0;
        private PartResourceDefinition _partRresourceDefinition1;
        private PartResourceDefinition _partRresourceDefinition2;

        BaseField _chooseField;
        BaseEvent _nextTankSetupEvent;
        BaseEvent _previousTankSetupEvent;

        List<string> currentResources;

        UIPartActionWindow tweakableUI;

        public virtual void OnRescale(TweakScale.ScalingFactor factor)
        {
            try
            {
                storedVolumeMultiplier = Mathf.Pow(factor.absolute.linear, volumeExponent);
                storedMassMultiplier = Mathf.Pow(factor.absolute.linear, massExponent);

                //part.heatConvectiveConstant = this.heatConvectiveConstant * (float)Math.Pow(factor.absolute.linear, 1);
                //part.heatConductivity = this.heatConductivity * (float)Math.Pow(factor.absolute.linear, 1);

                initialMass = part.prefabMass * storedMassMultiplier;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnRescale Error: " + e.Message);
                throw;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            try
            {
                initialMass = part.prefabMass * storedMassMultiplier;

                if (initialMass == 0)
                    initialMass = part.prefabMass;

                InitializeData();

                if (selectedTankSetup == -1)
                    selectedTankSetup = 0;

                this.enabled = true;

                if (state != StartState.Editor)
                {
                    //gameLoaded = true;
                    if (inFlightTankSetup == -1)
                        inFlightTankSetup = selectedTankSetup;
                    else
                        selectedTankSetup = inFlightTankSetup;
                }

                AssignResourcesToPart(false);

                _nextTankSetupEvent = Events["nextTankSetupEvent"];
                _previousTankSetupEvent = Events["previousTankSetupEvent"];

                _chooseField = Fields["selectedTankSetup"];
                _chooseField.guiName = switcherDescription;
                _chooseField.guiActiveEditor = availableInEditor && hasSwitchChooseOption;
                _chooseField.guiActive = false;

                var chooseOptionEditor = _chooseField.uiControlEditor as UI_ChooseOption;
                if (chooseOptionEditor != null)
                {
                    chooseOptionEditor.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                    chooseOptionEditor.onFieldChanged = UpdateFromGUI;
                }

                //var chooseOptionFlight = _chooseField.uiControlFlight as UI_ChooseOption;
                //if (chooseOptionFlight != null)
                //{
                //    chooseOptionFlight.options = _modularTankList.Select(s => s.SwitchName).ToArray();
                //    chooseOptionFlight.onFieldChanged = UpdateFromGUI;
                //}

                //Fields["partTemperatureStr"].guiActive = showTemperature;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnStart Error: " + e.Message);
                throw;
            }
        }

        private void UpdateFromGUI(BaseField field, object oldFieldValueObj)
        {
            if (!_modularTankList[selectedTankSetup].hasTech)
            {
                if ((int)oldFieldValueObj < selectedTankSetup || ((int)oldFieldValueObj == _modularTankList.Count - 1 && selectedTankSetup == 0))
                    nextTankSetupEvent();
                else
                    previousTankSetupEvent();
            }
            else
                AssignResourcesToPart(true);
        }

        // Called by external classes
        public int SelectTankSetup(int newTankIndex, bool calledByPlayer)
        {
            //Debug.Log("InsterstellarFuelSwitch SelectTankSetup called with newTankSetup = " + newTankIndex + " calledByPlayer = " + calledByPlayer);
            try
            {
                InitializeData();
                //Debug.Log("InsterstellarFuelSwitch SelectTankSetup has Initialised Data");

                if (selectedTankSetup == newTankIndex)
                {
                    //Debug.Log("InsterstellarFuelSwitch SelectTankSetup selectedTankSetup == newTankIndex");
                    return newTankIndex;
                }

                var oldSelectedTankSetup = selectedTankSetup;
                selectedTankSetup = newTankIndex;
                //Debug.Log("InsterstellarFuelSwitch SelectTankSetup has set selectedTankSetup to " + newTankIndex);

                if (!_modularTankList[selectedTankSetup].hasTech)
                {
                    if (oldSelectedTankSetup < selectedTankSetup || (oldSelectedTankSetup == _modularTankList.Count - 1 && selectedTankSetup == 0))
                        nextTankSetupEvent();
                    else
                        previousTankSetupEvent();
                }
                else
                {
                    AssignResourcesToPart(calledByPlayer);
                    //Debug.Log("InsterstellarFuelSwitch SelectTankSetup AssignResourcesToPart");
                }

                //Debug.Log("InsterstellarFuelSwitch SelectTankSetup Returns " + selectedTankSetup);
                return selectedTankSetup;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch SelectTankSetup Error: " + e.Message);
                throw;
            }
        }

        public override void OnAwake()
        {
            try
            {
                if (configLoaded)
                    InitializeData();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnAwake Error: " + e.Message);
                throw;
            }
        }

        public override void OnLoad(ConfigNode partNode)
        {
            try
            {
                base.OnLoad(partNode);

                if (!configLoaded)
                    InitializeData();

                configLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch OnLoad Error: " + e.Message);
                throw;
            }
        }

        private void InitializeData()
        {
            try
            {
                // Prevent execution to once per Scene switch
                if (initialized)
                    return;

                availableInEditor = String.IsNullOrEmpty(inEditorSwitchingTechReq) ? availableInEditor : hasTech(inEditorSwitchingTechReq);
                availableInFlight = String.IsNullOrEmpty(inFlightSwitchingTechReq) ? availableInFlight : hasTech(inFlightSwitchingTechReq);

                SetupTankList(false);

                if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight)
                {
                    Debug.Log("InsterstellarFuelSwitch Verify Tank Tech Requirements ");
                    foreach (var modularTank in _modularTankList)
                    {
                        modularTank.hasTech = hasTech(modularTank.techReq);
                    }
                }

                var nextEvent = Events["nextTankSetupEvent"];
                nextEvent.guiActive = hasGUI && availableInFlight;
                nextEvent.guiActiveEditor = hasGUI && availableInEditor;
                nextEvent.guiName = nextTankSetupText;

                var previousEvent = Events["previousTankSetupEvent"];
                previousEvent.guiActive = hasGUI && availableInFlight;
                previousEvent.guiActiveEditor = hasGUI && availableInEditor;
                previousEvent.guiName = previousTankSetupText;

                Fields["addedCost"].guiActiveEditor = displayCurrentTankCost && HighLogic.LoadedSceneIsEditor;

                if (useTextureSwitchModule)
                {
                    textureSwitch = part.GetComponent<InterstellarTextureSwitch2>(); // only looking for first, not supporting multiple fuel switchers
                    if (textureSwitch == null)
                    {
                        useTextureSwitchModule = false;
                        Debug.Log("no InterstellarTextureSwitch2 module found, despite useTextureSwitchModule being true");
                    }
                }

                initialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch InitializeData Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Next tank setup")]
        public void nextTankSetupEvent()
        {
            try
            {
                selectedTankSetup++;

                if (selectedTankSetup >= _modularTankList.Count)
                    selectedTankSetup = 0;

                if (!_modularTankList[selectedTankSetup].hasTech)
                    nextTankSetupEvent();

                AssignResourcesToPart(true);
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch nextTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Previous tank setup")]
        public void previousTankSetupEvent()
        {
            try
            {
                selectedTankSetup--;
                if (selectedTankSetup < 0)
                    selectedTankSetup = _modularTankList.Count - 1;

                if (!_modularTankList[selectedTankSetup].hasTech)
                    previousTankSetupEvent();

                AssignResourcesToPart(true);
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch previousTankSetupEvent Error: " + e.Message);
                throw;
            }
        }

        private void AssignResourcesToPart(bool calledByPlayer = false)
        {
            try
            {
                // destroying a resource messes up the gui in editor, but not in flight.
                currentResources = SetupTankInPart(part, calledByPlayer);

                // update GUI part
                ConfigureResourceMassGui(currentResources);
                UpdateTankName();
                UpdateTexture(calledByPlayer);

                // update Dry Mass
                UpdateDryMass();
                UpdateMassRatio();

                if (HighLogic.LoadedSceneIsEditor)
                {
                    foreach (var symPart in part.symmetryCounterparts)
                    {
                        var symNewResources = SetupTankInPart(symPart, calledByPlayer);

                        InterstellarFuelSwitch symSwitch = symPart.GetComponent<InterstellarFuelSwitch>();
                        if (symSwitch != null)
                        {
                            symSwitch.selectedTankSetup = selectedTankSetup;
                            symSwitch.selectedTankSetupTxt = selectedTankSetupTxt;
                            symSwitch.ConfigureResourceMassGui(symNewResources);
                            symSwitch.UpdateTankName();
                            symSwitch.UpdateTexture(calledByPlayer);
                        }
                    }
                }

                if (tweakableUI == null)
                    tweakableUI = part.FindActionWindow();

                if (tweakableUI != null)
                    tweakableUI.displayDirty = true;

            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch AssignResourcesToPart Error " + e.Message);
                throw;
            }
        }

        public void UpdateTexture(bool calledByPlayer)
        {
            //Debug.Log("InsterstellarFuelSwitch UpdateTexture");

            if (textureSwitch != null)
                textureSwitch.SelectTankSetup(selectedTankSetup, calledByPlayer);
        }

        public void UpdateTankName()
        {
            var selectedTank = _modularTankList[selectedTankSetup];
            tankGuiName = selectedTank.GuiName;
            Fields["tankGuiName"].guiActive = showTankName && !String.IsNullOrEmpty(tankGuiName);
            Fields["tankGuiName"].guiActiveEditor = showTankName && !String.IsNullOrEmpty(tankGuiName);


            //if (displayCurrentBoilOffTemp)
            //{
            //    Fields["currentBoiloffTempStr"].guiActive = true;
            //    Fields["currentBoiloffTempStr"].guiActiveEditor = true;
            //    currentBoiloffTempStr = selectedTank.Resources[0].boiloffTemp.ToString("0.00");
            //}
            //else
            //{
            //    Fields["currentBoiloffTempStr"].guiActive = false;
            //    Fields["currentBoiloffTempStr"].guiActiveEditor = false;
            //}
        }

        private List<string> SetupTankInPart(Part currentPart, bool calledByPlayer)
        {
            try
            {
                // find selected tank
                var selectedTank = calledByPlayer || String.IsNullOrEmpty(selectedTankSetupTxt)
                    ? selectedTankSetup < _modularTankList.Count ? _modularTankList[selectedTankSetup] : _modularTankList[0]
                    : _modularTankList.FirstOrDefault(t => t.GuiName == selectedTankSetupTxt) ?? (selectedTankSetup < _modularTankList.Count ? _modularTankList[selectedTankSetup] : _modularTankList[0]);

                // update txt and index for future
                selectedTankSetupTxt = selectedTank.GuiName;
                selectedTankSetup = _modularTankList.IndexOf(selectedTank);

                // create new ResourceNode
                var newResources = new List<string>();
                var newResourceNodes = new List<ConfigNode>();
                var parsedConfigAmount = new List<float>();

                // parse configured amounts
                if (configuredAmounts.Length > 0)
                {
                    // empty configuration if switched by user
                    if (calledByPlayer)
                    {
                        configuredAmounts = String.Empty;
                    }

                    string[] configAmount = configuredAmounts.Split(',');
                    foreach (string item in configAmount)
                    {
                        float value;
                        if (float.TryParse(item, out value))
                            parsedConfigAmount.Add(value);
                    }

                    // empty configuration if in flight
                    if (!HighLogic.LoadedSceneIsEditor)
                    {
                        configuredAmounts = String.Empty;
                    }
                }

                // imitialise minimum boiloff temperature at current part temperature
                //double minimumBoiloffTemerature = -1;

                for (int resourceId = 0; resourceId < selectedTank.Resources.Count; resourceId++)
                {
                    var selectedTankResource = selectedTank.Resources[resourceId];

                    //if (minimumBoiloffTemerature == -1 || (selectedTankResource.boiloffTemp > 0 && selectedTankResource.boiloffTemp < minimumBoiloffTemerature))
                    //    minimumBoiloffTemerature = selectedTankResource.boiloffTemp;

                    if (selectedTankResource.name == "Structural")
                        continue;

                    newResources.Add(selectedTankResource.name);

                    ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                    double maxAmount = selectedTankResource.maxAmount * storedVolumeMultiplier;

                    newResourceNode.AddValue("name", selectedTankResource.name);
                    newResourceNode.AddValue("maxAmount", maxAmount);

                    PartResource existingResource = null;
                    if (!HighLogic.LoadedSceneIsEditor || (HighLogic.LoadedSceneIsEditor && !calledByPlayer))
                    {
                        foreach (PartResource partResource in currentPart.Resources)
                        {
                            if (partResource.resourceName.Equals(selectedTankResource.name))
                            {
                                existingResource = partResource;
                                break;
                            }
                        }
                    }

                    double resourceNodeAmount;
                    if (existingResource != null)
                        resourceNodeAmount = Math.Min(existingResource.amount, maxAmount);
                    else if (!HighLogic.LoadedSceneIsEditor && resourceId < parsedConfigAmount.Count)
                        resourceNodeAmount = parsedConfigAmount[resourceId];
                    else if (!HighLogic.LoadedSceneIsEditor && calledByPlayer)
                        resourceNodeAmount = 0.0;
                    else
                        resourceNodeAmount = selectedTank.Resources[resourceId].amount * storedVolumeMultiplier;

                    newResourceNode.AddValue("amount", resourceNodeAmount);
                    newResourceNodes.Add(newResourceNode);
                }


                //// prepare part to initialise temerature 
                //if (minimumBoiloffTemerature != -1)
                //{
                //    var currentFuelswitch = part.FindModuleImplementing<InterstellarFuelSwitch>();
                //    if (currentFuelswitch != null)
                //    {
                //        Debug.Log("InsterstellarFuelSwitch SetupTankInPart prepared to initialise part temperature at " + minimumBoiloffTemerature);
                //        currentFuelswitch.initializePartTemperature = minimumBoiloffTemerature;
                //    }
                //}

                var finalResourceNodes = new List<ConfigNode>();

                // remove all resources except those we ignore
                PartResource[] partResources = currentPart.GetComponents<PartResource>();
                foreach (PartResource resource in partResources)
                {
                    if (activeResourceList.Contains(resource.resourceName))
                    {
                        if (newResourceNodes.Count > 0)
                        {
                            finalResourceNodes.AddRange(newResourceNodes);
                            newResourceNodes.Clear();
                        }

                        currentPart.Resources.list.Remove(resource);
                        DestroyImmediate(resource);
                    }
                    else
                    {
                        ConfigNode newResourceNode = new ConfigNode("RESOURCE");
                        newResourceNode.AddValue("name", resource.resourceName);
                        newResourceNode.AddValue("maxAmount", resource.maxAmount);
                        newResourceNode.AddValue("amount", resource.amount);

                        finalResourceNodes.Add(newResourceNode);
                        Debug.Log("InsterstellarFuelSwitch SetupTankInPart created confignode for: " + resource.resourceName);

                        // remove all
                        currentPart.Resources.list.Remove(resource);
                        DestroyImmediate(resource);
                        Debug.Log("InsterstellarFuelSwitch SetupTankInPart remove resource " + resource.resourceName);
                    }
                }

                // add any remaining bew nodes
                if (newResourceNodes.Count > 0)
                {
                    finalResourceNodes.AddRange(newResourceNodes);
                    newResourceNodes.Clear();
                }

                // add new resources
                if (finalResourceNodes.Count > 0)
                {
                    Debug.Log("InsterstellarFuelSwitch SetupTankInPart adding resources: " + ParseTools.Print(newResources));
                    foreach (var resourceNode in finalResourceNodes)
                    {
                        currentPart.AddResource(resourceNode);
                    }
                }

                // This also needs to be done when going from a setup with resources to a setup with no resources.
                currentPart.Resources.UpdateList();
                UpdateCost();

                return newResources;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch SetupTankInPart Error: " + e.Message);
                throw;
            }
        }

        public void ConfigureResourceMassGui(List<string> newResources)
        {
            _partRresourceDefinition0 = newResources.Count > 0 ? PartResourceLibrary.Instance.GetDefinition(newResources[0]) : null;
            _partRresourceDefinition1 = newResources.Count > 1 ? PartResourceLibrary.Instance.GetDefinition(newResources[1]) : null;
            _partRresourceDefinition2 = newResources.Count > 2 ? PartResourceLibrary.Instance.GetDefinition(newResources[2]) : null;

            var field0 = Fields["resourceAmountStr0"];
            var field1 = Fields["resourceAmountStr1"];
            var field2 = Fields["resourceAmountStr2"];

            field0.guiName = _partRresourceDefinition0 != null ? _partRresourceDefinition0.name : ":";
            field1.guiName = _partRresourceDefinition1 != null ? _partRresourceDefinition1.name : ":";
            field2.guiName = _partRresourceDefinition2 != null ? _partRresourceDefinition2.name : ":";

            field0.guiActive = _partRresourceDefinition0 != null;
            field1.guiActive = _partRresourceDefinition1 != null;
            field2.guiActive = _partRresourceDefinition2 != null;

            field0.guiActiveEditor = _partRresourceDefinition0 != null;
            field1.guiActiveEditor = _partRresourceDefinition1 != null;
            field2.guiActiveEditor = _partRresourceDefinition2 != null;

            _partResource0 = _partRresourceDefinition0 == null ? null : part.Resources.list.FirstOrDefault(r => r.resourceName == _partRresourceDefinition0.name);
            _partResource1 = _partRresourceDefinition1 == null ? null : part.Resources.list.FirstOrDefault(r => r.resourceName == _partRresourceDefinition1.name);
            _partResource2 = _partRresourceDefinition2 == null ? null : part.Resources.list.FirstOrDefault(r => r.resourceName == _partRresourceDefinition2.name);
        }

        private float UpdateCost()
        {
            try
            {
                if (selectedTankSetup >= 0 && selectedTankSetup < tankCostList.Count)
                {
                    addedCost = (float)tankCostList[selectedTankSetup];
                    return addedCost;
                }

                addedCost = 0;
                if (_partRresourceDefinition0 == null || _partResource0 == null) return addedCost;
                addedCost += _partRresourceDefinition0.unitCost * (float)_partResource0.maxAmount;

                if (_partRresourceDefinition1 == null || _partResource1 == null) return addedCost;
                addedCost += _partRresourceDefinition1.unitCost * (float)_partResource1.maxAmount;

                if (_partRresourceDefinition2 != null && _partResource2 != null)
                    addedCost += _partRresourceDefinition2.unitCost * (float)_partResource2.maxAmount;

                return addedCost;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch UpdateCost Error: " + e.Message);
                throw;
            }
        }

        private void UpdateDryMass()
        {
            // update Dry Mass
            dryMass = CalculateDryMass(HighLogic.LoadedSceneIsFlight ? inFlightTankSetup : selectedTankSetup);
        }

        private double CalculateDryMass(int tankSetupIndex)
        {
            double mass = basePartMass;
            //Debug.LogError("CalculateDryMass basePartMass: " + basePartMass);

            // use tankMass if defined
            if (weightList != null && tankSetupIndex >= 0 && tankSetupIndex < weightList.Count)
            {
                mass += weightList[tankSetupIndex];
                //Debug.LogError("CalculateDryMass UpdateCost weight: " + weightList[tankSetupIndex]);
            }

            // use baseResourceMassDivider if specified
            if (baseResourceMassDivider > 0 && tankSetupIndex >= 0 && tankSetupIndex < _modularTankList.Count)
            {
                mass += _modularTankList[tankSetupIndex].FullMass / baseResourceMassDivider;
                //Debug.LogError("CalculateDryMass FullMass: " + _modularTankList[tankSetupIndex].FullMass);
                //Debug.LogError("CalculateDryMass baseResourceMassDivider: " + baseResourceMassDivider);
            }

            // use tankResourceMassDividerList if specified
            if (tankSetupIndex >= 0 && tankSetupIndex < _modularTankList.Count && tankSetupIndex < tankResourceMassDividerList.Count && tankResourceMassDividerList[tankSetupIndex] > 0)
            {
                mass += _modularTankList[tankSetupIndex].FullMass / tankResourceMassDividerList[tankSetupIndex];
                //Debug.LogError("CalculateDryMass FullMass: " + _modularTankList[tankSetupIndex].FullMass);
                //Debug.LogError("CalculateDryMass tankResourceMassDivider: " + tankResourceMassDividerList[tankSetupIndex]);
            }

            // prevent 0 mass
            if (mass == 0)
            {
                //Debug.LogError("CalculateDryMass Mass == 0 setting mass to " + initialMass);
                mass = initialMass;
            }

            return mass * storedMassMultiplier;
        }

        private string formatMassStr(double amount)
        {
            try
            {
                if (amount >= 1)
                    return (amount).ToString(resourcesFormat) + " t";
                if (amount >= 1e-3)
                    return (amount * 1e3).ToString(resourcesFormat) + " kg";
                if (amount >= 1e-6)
                    return (amount * 1e6).ToString(resourcesFormat) + " g";

                return (amount * 1e9).ToString(resourcesFormat) + " mg";
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch formatMassStr Error: " + e.Message);
                throw;
            }
        }

        private void UpdateGuiResourceMass()
        {
            var missing0 = _partRresourceDefinition0 == null || _partResource0 == null;
            var missing1 = _partRresourceDefinition1 == null || _partResource1 == null;
            var missing2 = _partRresourceDefinition2 == null || _partResource2 == null;

            var currentResourceMassAmount0 = missing0 ? 0 : _partRresourceDefinition0.density * _partResource0.amount;
            var currentResourceMassAmount1 = missing1 ? 0 : _partRresourceDefinition1.density * _partResource1.amount;
            var currentResourceMassAmount2 = missing2 ? 0 : _partRresourceDefinition2.density * _partResource2.amount;

            wetMass = currentResourceMassAmount0 + currentResourceMassAmount1 + currentResourceMassAmount2;
            totalMass = dryMass + wetMass;

            resourceAmountStr0 = missing0 ? String.Empty : formatMassStr(currentResourceMassAmount0);
            resourceAmountStr1 = missing1 ? String.Empty : formatMassStr(currentResourceMassAmount1);
            resourceAmountStr2 = missing2 ? String.Empty : formatMassStr(currentResourceMassAmount2);
        }

        private void UpdateMassRatio()
        {
            var maxResourceMassAmount0 = _partRresourceDefinition0 == null || _partResource0 == null ? 0 : _partRresourceDefinition0.density * _partResource0.maxAmount;
            var maxResourceMassAmount1 = _partRresourceDefinition1 == null || _partResource1 == null ? 0 : _partRresourceDefinition1.density * _partResource1.maxAmount;
            var maxResourceMassAmount2 = _partRresourceDefinition2 == null || _partResource2 == null ? 0 : _partRresourceDefinition2.density * _partResource2.maxAmount;

            var maxWetMass = maxResourceMassAmount0 + maxResourceMassAmount1 + maxResourceMassAmount2;

            if (part.mass > 0 && maxWetMass > 0)
                massRatioStr = ToRoundedString(1 / (dryMass / maxWetMass));
        }

        private string ToRoundedString(double value)
        {
            var massRatioRounded = Math.Round(value, 0);
            var differenceWithRounded = Math.Abs(value - massRatioRounded);

            if (differenceWithRounded > 0.05)
                return "1 : " + value.ToString("0.0");
            else if (differenceWithRounded > 0.005)
                return "1 : " + value.ToString("0.00");
            else if (differenceWithRounded > 0.0005)
                return "1 : " + value.ToString("0.000");
            else
                return "1 : " + value.ToString("0");
        }

        public override void OnUpdate()
        {
            if (initializePartTemperature != -1 && initializePartTemperature > 0)
            {
                //Debug.Log("InsterstellarFuelSwitch OnUpdate initialise part temperature at " + initializePartTemperature);
                part.temperature = initializePartTemperature;
                initializePartTemperature = -1;
                traceBoiloff = true;
            }


        }

        public void ProcessBoiloff()
        {
            try
            {
                if (!boiloffActive) return;

                if (!traceBoiloff) return;

                if (_modularTankList == null) return;

                var currentTemperature = part.temperature;
                var selectedTank = _modularTankList[selectedTankSetup];
                foreach (var resource in selectedTank.Resources)
                {
                    if (currentTemperature <= resource.boiloffTemp) continue;

                    var deltaTemperatureDifferenceInKelvin = currentTemperature - resource.boiloffTemp;

                    PartResource partResource = part.Resources.list.FirstOrDefault(r => r.resourceName == resource.name);
                    PartResourceDefinition resourceDefinition = PartResourceLibrary.Instance.GetDefinition(resource.name);

                    if (resourceDefinition == null || partResource == null) continue;

                    specificHeatStr = resourceDefinition.specificHeatCapacity.ToString("0.0000");

                    var specificHeat = resourceDefinition.specificHeatCapacity > 0 ? resourceDefinition.specificHeatCapacity : 1000;

                    //var standardSpecificHeatCapacity = 800;
                    // calcualte boiloff
                    //var wetMass = partResource.amount * resourceDefinition.density;
                    //var drymass = CalculateDryMass(selectedTankSetup);
                    //var ThermalMass =  (drymass * standardSpecificHeatCapacity * part.thermalMassModifier) + (wetMass * specificHeat);

                    var ThermalMass = part.thermalMass;
                    var heatAbsorbed = ThermalMass * deltaTemperatureDifferenceInKelvin;

                    this.boiloffEnergy = (heatAbsorbed / TimeWarp.fixedDeltaTime).ToString("0.0000") + " kJ/s";

                    var latendHeatVaporation = resource.latendHeatVaporation != 0 ? resource.latendHeatVaporation * 1000 : specificHeat * 34;

                    var resourceBoilOff = heatAbsorbed / latendHeatVaporation;
                    var boiloffAmount = resourceBoilOff / resourceDefinition.density;

                    // reduce boiloff from part  
                    partResource.amount = Math.Max(0, partResource.amount - boiloffAmount);
                    part.temperature = resource.boiloffTemp;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch ProcessBoiloff Error: " + e.Message);
                throw;
            }
        }

        ////public override void OnFixedUpdate()
        ////{
        ////    currentPartMass = part.mass;

        ////    ProcessBoiloff();

        ////    partTemperatureStr = part.temperature + " K";

        ////    base.OnFixedUpdate();
        ////}

        // Note: do note remove, it is called by KSP
        public void Update()
        {
            currentPartMass = part.mass;

            partTemperatureStr = part.temperature + " K";

            if (currentResources != null)
                ConfigureResourceMassGui(currentResources);

            if (HighLogic.LoadedSceneIsFlight)
            {
                UpdateGuiResourceMass();

                //There were some issues with resources slowly trickling in, so I changed this to 0.1% instead of empty.
                isEmpty = !part.Resources.list.Any(r => r.amount > r.maxAmount / 1000);
                var showSwitchButtons = availableInFlight && isEmpty;

                //_chooseField.guiActive = showSwitchButtons;
                _nextTankSetupEvent.guiActive = showSwitchButtons;
                _previousTankSetupEvent.guiActive = showSwitchButtons;

                return;
            }

            // update Dry Mass
            UpdateDryMass();
            UpdateGuiResourceMass();
            UpdateMassRatio();

            configuredAmounts = "";
            foreach (var resoure in part.Resources.list)
            {
                configuredAmounts += resoure.amount + ",";
            }
        }

        private void SetupTankList(bool calledByPlayer)
        {
            try
            {
                weightList = ParseTools.ParseDoubles(tankMass, () => tankMass);
                tankCostList = ParseTools.ParseDoubles(tankCost, () => tankCost);
                tankResourceMassDividerList = ParseTools.ParseDoubles(tankResourceMassDivider, () => tankResourceMassDivider);

                // First find the amounts each tank type is filled with
                List<List<double>> resourceList = new List<List<double>>();
                List<List<double>> initialResourceList = new List<List<double>>();
                List<List<double>> boilOffTempList = new List<List<double>>();
                List<List<double>> latendHeatVaporationList = new List<List<double>>();

                string[] resourceTankAbsoluteAmountArray = resourceAmounts.Split(';');
                string[] resourceTankRatioAmountArray = resourceRatios.Split(';');
                string[] initialResourceTankArray = initialResourceAmounts.Split(';');
                string[] boilOffTempTankArray = boilOffTemp.Split(';');
                string[] latendHeatVaporationArray = latendHeatVaporation.Split(';');
                string[] tankNameArray = resourceNames.Split(';');
                string[] tankTechReqArray = tankTechReq.Split(';');
                string[] tankGuiNameArray = resourceGui.Split(';');
                string[] tankSwitcherNameArray = tankSwitchNames.Split(';');

                // if initial resource ammount is missing or not complete, use full amount
                if (initialResourceAmounts.Equals(String.Empty) ||
                    initialResourceTankArray.Length != resourceTankAbsoluteAmountArray.Length)
                    initialResourceTankArray = resourceTankAbsoluteAmountArray;

                var maxLengthTankArray = Math.Max(resourceTankAbsoluteAmountArray.Length, resourceTankRatioAmountArray.Length);

                for (int tankCounter = 0; tankCounter < maxLengthTankArray; tankCounter++)
                {
                    resourceList.Add(new List<double>());
                    initialResourceList.Add(new List<double>());
                    boilOffTempList.Add(new List<double>());
                    latendHeatVaporationList.Add(new List<double>());

                    string[] resourceAmountArray = resourceTankAbsoluteAmountArray[tankCounter].Trim().Split(',');
                    string[] initialResourceAmountArray = initialResourceTankArray[tankCounter].Trim().Split(',');
                    string[] boilOffTempAmountArray = boilOffTempTankArray.Count() > tankCounter ? boilOffTempTankArray[tankCounter].Trim().Split(',') : new string[0];
                    string[] latendHeatVaporationAmountArray = latendHeatVaporationArray.Count() > tankCounter ? latendHeatVaporationArray[tankCounter].Trim().Split(',') : new string[0];

                    // if missing or not complete, use full amount
                    if (initialResourceAmounts.Equals(String.Empty) ||
                        initialResourceAmountArray.Length != resourceAmountArray.Length)
                        initialResourceAmountArray = resourceAmountArray;

                    for (var amountCounter = 0; amountCounter < resourceAmountArray.Length; amountCounter++)
                    {
                        try
                        {
                            if (tankCounter >= resourceList.Count || amountCounter >= resourceAmountArray.Count()) continue;

                            resourceList[tankCounter].Add(double.Parse(resourceAmountArray[amountCounter].Trim()));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing resourceTankAmountArray amount " + tankCounter + "/" + amountCounter +
                                      ": '" + resourceTankAbsoluteAmountArray[tankCounter] + "': '" + resourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < initialResourceList.Count && amountCounter < initialResourceAmountArray.Count())
                                initialResourceList[tankCounter].Add(ParseTools.ParseDouble(initialResourceAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing initialResourceList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + initialResourceList[tankCounter] + "': '" + initialResourceAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < boilOffTempList.Count && amountCounter < boilOffTempAmountArray.Length)
                                boilOffTempList[tankCounter].Add(ParseTools.ParseDouble(boilOffTempAmountArray[amountCounter]));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing boilOffTempList amount " + tankCounter + "/" + amountCounter +
                                      ": '" + boilOffTempList[tankCounter] + "': '" + boilOffTempAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }

                        try
                        {
                            if (tankCounter < latendHeatVaporationList.Count && amountCounter < latendHeatVaporationAmountArray.Length)
                                latendHeatVaporationList[tankCounter].Add(ParseTools.ParseDouble(latendHeatVaporationAmountArray[amountCounter].Trim()));
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("InsterstellarFuelSwitch: error parsing latendHeatVaporation amount " + tankCounter + "/" + amountCounter +
                                      ": '" + latendHeatVaporationList[tankCounter] + "': '" + latendHeatVaporationAmountArray[amountCounter].Trim() + "' with error: " + exception.Message);
                        }
                    }
                }

                // Then find the kinds of resources each tank holds, and fill them with the amounts found previously, or the amount hey held last (values kept in save persistence/craft)
                for (int currentResourceCounter = 0; currentResourceCounter < tankNameArray.Length; currentResourceCounter++)
                {
                    // create a new modularTank
                    var modularTank = new IFSmodularTank();
                    _modularTankList.Add(modularTank);

                    // initialiseSwitchName
                    if (currentResourceCounter < tankSwitcherNameArray.Length)
                        modularTank.SwitchName = tankSwitcherNameArray[currentResourceCounter];

                    // initialize Gui name if possible
                    if (currentResourceCounter < tankGuiNameArray.Length)
                        modularTank.GuiName = tankGuiNameArray[currentResourceCounter];

                    // initialise tech requirement but ignore first
                    if (currentResourceCounter != 0 && currentResourceCounter < tankTechReqArray.Length)
                        modularTank.techReq = tankTechReqArray[currentResourceCounter].Trim(' ');

                    // initialise tank mass
                    if (currentResourceCounter < weightList.Count)
                        modularTank.tankMass = weightList[currentResourceCounter];

                    if (currentResourceCounter < tankResourceMassDividerList.Count)
                        modularTank.resourceMassDivider = tankResourceMassDividerList[currentResourceCounter];

                    // initialise tank cost
                    if (currentResourceCounter < tankCostList.Count)
                        modularTank.tankCost = tankCostList[currentResourceCounter];

                    string[] resourceNameArray = tankNameArray[currentResourceCounter].Split(',');
                    for (var nameCounter = 0; nameCounter < resourceNameArray.Length; nameCounter++)
                    {
                        var resourceName = resourceNameArray[nameCounter].Trim(' ');
                        var newResource = new IFSresource(resourceName);

                        if (!activeResourceList.Contains(resourceName))
                            activeResourceList.Add(resourceName);

                        if (resourceList[currentResourceCounter] != null && nameCounter < resourceList[currentResourceCounter].Count)
                        {
                            newResource.maxAmount = resourceList[currentResourceCounter][nameCounter];
                            newResource.amount = initialResourceList[currentResourceCounter][nameCounter];
                        }

                        // add boiloff data
                        if (currentResourceCounter < boilOffTempList.Count && boilOffTempList[currentResourceCounter] != null && boilOffTempList[currentResourceCounter].Count > nameCounter)
                            newResource.boiloffTemp = boilOffTempList[currentResourceCounter][nameCounter];

                        if (currentResourceCounter < latendHeatVaporationList.Count && latendHeatVaporationList[currentResourceCounter] != null && latendHeatVaporationList[currentResourceCounter].Count > nameCounter)
                            newResource.latendHeatVaporation = latendHeatVaporationList[currentResourceCounter][nameCounter];

                        modularTank.Resources.Add(newResource);
                    }

                    // ensure there is always a gui name
                    if (string.IsNullOrEmpty(modularTank.GuiName))
                    {
                        var names = modularTank.Resources.Select(m => m.name);
                        modularTank.GuiName = String.Empty;
                        foreach (var name in names)
                        {
                            if (!String.IsNullOrEmpty(modularTank.GuiName))
                                modularTank.GuiName += "+";
                            modularTank.GuiName += name;
                        }
                    }

                    // use guiTankName is switchName is missing
                    if (string.IsNullOrEmpty(modularTank.SwitchName))
                        modularTank.SwitchName = modularTank.GuiName;
                }

                var maxNrTanks = _modularTankList.Max(t => t.Resources.Count);
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch SetupTankList Error: " + e.Message);
                throw;
            }
        }

        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            try
            {
                return UpdateCost();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch GetModuleCost Error:" + e.Message);
                throw;
            }
        }

        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.STAGED;
        }

        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            try
            {
                this.defaultMass = defaultMass;

                UpdateDryMass();
                UpdateMassRatio();

                moduleMassDelta = dryMass - initialMass;

                return (float)moduleMassDelta;
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch GetModuleMass Error: " + e.Message);
                throw;
            }
        }

        public override string GetInfo()
        {
            try
            {
                if (!showInfo) return string.Empty;

                var info = new StringBuilder();

                info.AppendLine("Fuel tank setups available:");
                info.AppendLine();

                foreach (var module in _modularTankList)
                {
                    int count = 0;
                    info.Append("* ");

                    foreach (var resource in module.Resources)
                    {
                        if (count > 0)
                            info.Append(" + ");
                        if (resource.maxAmount > 0)
                        {
                            info.Append(resource.maxAmount);
                            info.Append(" ");
                        }
                        info.Append(resource.name);

                        count++;
                    }

                    info.AppendLine();
                }
                return info.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError("InsterstellarFuelSwitch GetInfo Error " + e.Message);
                throw;
            }
        }

        private bool hasTech(string techid)
        {
            if (String.IsNullOrEmpty(techid))
                return true;

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return true;

            if ((HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX))
                return true;

            if (ResearchAndDevelopment.Instance == null)
            {
                if (researchedTechs == null)
                    LoadSaveFile();

                bool found = researchedTechs.Contains(techid);
                return found;
            }

            var techstate = ResearchAndDevelopment.Instance.GetTechState(techid);
            if (techstate != null)
            {
                var available = techstate.state == RDTech.State.Available;
                return available;
            }
            else
                return false;
        }

        private static HashSet<string> researchedTechs;

        private void LoadSaveFile()
        {
            researchedTechs = new HashSet<string>();

            string persistentfile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
            ConfigNode config = ConfigNode.Load(persistentfile);
            ConfigNode gameconf = config.GetNode("GAME");
            ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");

            foreach (ConfigNode scenario in scenarios)
            {
                if (scenario.GetValue("name") == "ResearchAndDevelopment")
                {
                    ConfigNode[] techs = scenario.GetNodes("Tech");
                    foreach (ConfigNode technode in techs)
                    {
                        var technodename = technode.GetValue("id");
                        researchedTechs.Add(technodename);
                    }
                }
            }
        }
    }
}
