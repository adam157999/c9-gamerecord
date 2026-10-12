In this solution, you only allow to use the method in .NET 4.8 framework to write controller, model and entityframework for database connection. All the naming convention must follow camelCase style. 
Understand the project structure, by given example as below, xxx is the name of the module, xxxConnectors the all the base function that contain actual implementation and calling the function from the Gamess folder. 
Under the Gamess folder in the workspace, its base function include API calling from the client site, custom function eg. md5 encryption. xxxController is not necssary calling the 
function implemented in the xxxConnectors unless instructor given, but both xxx name must be the same. In the last step, function in the xxxConnector need to be override the abstract function in the BaseConnector and those function 
will implement in the GameAPIController. The last, any new gameset added in must add the case in the switch function in the GetConnector function in GameAPIController. Folow the **Important** sections when generate the code and receive
the prompt to create a **new Game**.

**Important:** For every new game integration, you must:
1. Follow strict the reference code given in the chat.
2. Follow the **steps** to create the corresponding file in sequences in ascending order.
3. **All naming (xxx) must exactly match the game name provided in the prompt** - use the exact name given by the user for all files, classes, and functions (e.g., if user says "NewGame", use NewGameConnector, NewGameController, etc.)

**steps**
1. Create xxxConnectors in the folder Controller/v2. example output of file will be simiarl as Controller/v2/xxxConnectors.cs
2. Add the game code to the switch statement in the GetConnector function in GameAPIController.cs.
3. Add the game code to the switch statement in the FormGamePlayerID function in GameAPIController.cs.
4. Create xxxController in the folder Controller example output of file will be same as Controller/xxxController. Only do this steps when required
5. Create xxxViews in the Views folder with the xxx name example output of file will be same as Views/xxx/xxxController. Only do this steps when required

This ensures proper connector instantiation and player ID generation for each new game.

**Reference Files for New Game Creation:**
When creating a new game, MUST reference the following OctoPlay implementation as the style guide and example:
1. **API Base Implementation & Models**: C:\Users\pc\Documents\GitHub\c9-gamerecord\GameRecord3\GameRecord\Gamess\OctoPlay
2. **Connector Implementation**: C:\Users\pc\Documents\GitHub\c9-gamerecord\GameRecord3\GameRecord\Controllers\V2\OctoPlayConnector.cs
3. **Controller Implementation**: C:\Users\pc\Documents\GitHub\c9-gamerecord\GameRecord3\GameRecord\Controllers\OctoPlayController

Before starting any new game integration:
- Review the OctoPlay folder structure and all .cs files within
- Study the OctoPlayConnector.cs implementation pattern
- Examine the OctoPlayController.cs structure (if controller is required)
- Follow the EXACT same coding style, naming conventions, and architecture patterns
- The API reference documentation will be imported separately for each new game integration


