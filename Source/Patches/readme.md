We use HarmonyPatch to integrate this mod's custom code with the game's code at the following integration points.

- **CarAI::PathfindFailure**: We record building to building path finding failures, so that we temporarily exclude matches between these buildings.
- **DistrictManager::ReleaseDistrictImplementation**: Remove any references to the district that has been just erased.
- **DistrictManager::ReleaseParkImplementation**:  Remove any references to the campus/industry/park that has been just erased.
- **TransferManager::AddIncomingOffer**:  Adjusts the priority of offers for supported materials, as well as filter out certain taxi and park maintenance offers.
- **TransferManager::AddOutgoingOffer**:  Adjusts the priority of offers for supported materials, as well as filter out certain park maintenance offers.
- **TransferManager::MatchOffers**:  Calls our custom TransferManagerMod.MatchOffers for supported materials.

In addition, we directly interact with game objects in the following ways:

- Hook into BuildingManager.instance.EventBuildingCreated and BuildingManager.instance.EventBuildingReleased events to add or remove references to buildings.
- TransferManagerMod references various objects within the TransferManager that hold the incoming and outgoing offers.  This enables the MatchOffers method to match these offers, and frees us from having to create data structures to hold these offers ourselves.
