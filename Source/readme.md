# EnhancedDistrictServices
The project is simple enough such that all the CS files are in one directory.

## Classes
- **EnhancedDistrictServicesLoadingExtension**: Create the EnhancedDistrictServicesTool object. 
- **EnhancedDistrictServicesMod**: Main mod class that implements **IUserMod** and defines various metadata about the mod.
- **EnhancedDistrictServicesThreadingExtension**: Allows the user to activate the EnhancedDistrictServices tool by pressing Ctrl-D.
- **EnhancedDistrictServicesTool**: Derives from DefaultTool.  Activates the main panel and also allows users to mouse over and get summary info on buildings.

- **EnhancedDistrictServicesUIPanelBase**: Base class that defines all the GUI elements of the main panel.  Logic implemented separately in **EnhancedDistrictServicesUIPanel**.
- **Logger**: Yet another logger class ... 
- **TransferManagerAddOfferPatch**: Modifies the priority and amount of incoming and outgoing orders, including deprioritizing outside connection orders.
- **TransferManagerInfo**: Helper class containing methods for classifying buildings and offers.
- **TransferManagerMatchOffersPatch**: Overrides the offer matching algorithm in the TransferManager for supported materials, applying district and supply chain constraints.
