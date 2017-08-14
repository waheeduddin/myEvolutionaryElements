using UnityEngine;
using System.Collections.Generic;

public class TileMap : MonoBehaviour {

	public GameObject selectedUnit;

	public TileType[] tileTypes;
	
	public CollectableObjectType myCollectable;
	
	private bool playGame = false;
	int[,] tiles;
	//Node[,] graph;

	public int mapSizeX;
	public int mapSizeY;
    private int solverSteps = 0;

    //2nd time solver values
    private int solverSteps2 = 0;
    int[,] copyTiles;
    int playerPosX;
    int playerPosY;

    //game values
    bool ai = true; // to give control to the ai or not.
    int movesTaken = 0;//current number of moves
    int moveLimits = 100; //(max at the map.mapSizeX * map.mapSizeY)
    Genome moveAlgorithm;//contain the values of the genome
    bool inspectMoveSelection = false; //do we choose to select our next move? will be true mostly.
    bool playerDied = false; //the player is dead so do the graphics.
    int numOfBlockedTiles = 0; //to store how many of the would be traversed values are blocked already + have been backtracked. A tile that is enroute to the solution is not included here.
    int numOfExtraTiles = 0; //stores number of still open tiles that can have riches. they are the tiles that have been backtracked.

    //Evolution values. 
    int populationSize = 80;
    List<Genome> genomes; //an array of what?
    Genome algorithmGenome;
    int currentGenome = -1; // index of current genome
    int generation = 0; //which generation are we in
    List<Record> archive;
    float mutationRate = 0.05f;
    float mutationStep = 0.2f;

    void Start() {
        algorithmGenome = new Genome();
        Genome testGene;
        testGene = new Genome();
        testGene.avoidedDeath = 0.2f;
        testGene.revisitedSteps = 0.87f;
        testGene.collisionWithObstacle = 0.9f;
        genomes = new List<Genome>();
        genomes.Add(testGene);
        selectedUnit.GetComponent<Unit>().tileX = (int)selectedUnit.transform.position.x;
        selectedUnit.GetComponent<Unit>().tileY = (int)selectedUnit.transform.position.y;
        selectedUnit.GetComponent<Unit>().map = this;

        GenerateMapData();
		//GeneratePathfindingGraph();
		GenerateMapVisual();
	}

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.T)){
            launchObjects(); 
        }
        if (Input.GetKeyUp(KeyCode.B))
        {
            Debug.Log("calling solver hmm...");
            firstCustom();
        }
        if (Input.GetKeyUp(KeyCode.N))
        {
            Debug.Log("calling solver2 hmm...");
            moveLogic();
        }
    }
	void GenerateMapData() {
		// Allocate our map tiles
		tiles = new int[mapSizeX,mapSizeY];
        copyTiles = new int[mapSizeX, mapSizeY];
        int x,y;
		
		// Initialize our map tiles to be grass
		for(x=0; x < mapSizeX; x++) {
			for(y=0; y < mapSizeX; y++) {
                tiles[x, y] = 0;
                copyTiles[x, y] = 0;
                if ((x == 0) || (x == (mapSizeX - 1))) { 
                    tiles[x, y] = 1;
                    copyTiles[x, y] = 1;
                }
                if ((y == 0) || (y == (mapSizeY - 1))) { 
                    tiles[x, y] = 1;
                    copyTiles[x, y] = 1;
                }
            }
		}
        if((mapSizeX == 5 ) && (mapSizeY == 5))
        {
            tiles[2, 2] = 1;
            copyTiles[2, 2] = 1;
        
        }
	}
    
	void GenerateMapVisual() {
		for(int x=0; x < mapSizeX; x++) {
			for(int y=0; y < mapSizeX; y++) {
				TileType tt = tileTypes[ tiles[x,y] ];
				GameObject go = (GameObject)Instantiate( tt.tileVisualPrefab, new Vector3(x, y, 0), Quaternion.identity );

				ClickableTile ct = go.GetComponent<ClickableTile>();
				ct.tileX = x;
				ct.tileY = y;
				ct.map = this;
			}
		}
	}

	public Vector3 TileCoordToWorldCoord(int x, int y) {
		return new Vector3(x, y, 0);
	}

    public void MoveSelectedUnitTo(int x, int y) { 
        if ((tiles[x, y] != 2) && (tiles[x, y] != -1)) {  // blockedByMountain or Deleted
            selectedUnit.GetComponent<Unit>().tileX = x;
            selectedUnit.GetComponent<Unit>().tileY = y;
            selectedUnit.GetComponent<Unit>().map = this;
            selectedUnit.transform.position = TileCoordToWorldCoord(x, y);
        }
	}
    public void deleteBlock(int _x, int _y ){
	if(!playGame || ((_x == selectedUnit.transform.position.x) && (_y == selectedUnit.transform.position.y)))
        {
		tiles[_x , _y] = -1;
            //copyTiles[_x, _y] = -1;
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
		foreach(GameObject go in allObjects){
            if((go.transform.position.x == _x) && (go.transform.position.y == _y) && (go.transform.position.z == 0.0f))
                {
				Destroy(go);
		    	}
	    	}
	    }
    }
    public void createBlock(int _x , int _y , int _type)    {
	if(!playGame || ((_x == selectedUnit.transform.position.x) && (_y == selectedUnit.transform.position.y)))
        {
        	tiles[_x, _y] = _type;
            if((_type !=4) && (_type != 5))
                copyTiles[_x, _y] = _type;
            TileType tt = tileTypes[_type];
        	GameObject go = (GameObject)Instantiate(tt.tileVisualPrefab, new Vector3(_x, _y, 0), Quaternion.identity);

        	ClickableTile ct = go.GetComponent<ClickableTile>();
        	ct.tileX = _x;
        	ct.tileY = _y;
        	ct.map = this;
    	}
	}

    public void firstCustom()
    {
        for (int x = 0; x < mapSizeX; x++)
            for (int y = 0; y < mapSizeY; y++)
            {
                if(tiles[x,y] != 1) { 
                    Debug.Log("x:" + x + " y:" + y + " is tile " + tiles[x,y]);
                    Debug.Log("x:" + x + " y:" + y + " is copyTile" + copyTiles[x, y]);
                }
            }

                playerPosX = (int)selectedUnit.transform.position.x;
        playerPosY = (int)selectedUnit.transform.position.y;

        Debug.Log("calling solver...");
        if (solver((int)selectedUnit.transform.position.x, (int)selectedUnit.transform.position.y))
            Debug.Log("sovler solved it");
        else
            Debug.Log("not solved by solver 1..");
        
        for (int x = 0; x < mapSizeX; x++)
            for (int y = 0; y < mapSizeY; y++)
                if ((tiles[x, y] == 5) || (tiles[x, y] == 2)){
                   // tiles[x, y] = 5;//a fix.... the trap that is part of extra tiles. Maybe this is becoming true for all traps
                    numOfExtraTiles++;
                }
        for (int x = 0; x < mapSizeX; x++)
            for (int y = 0; y < mapSizeY; y++)
            {
                if ((tiles[x, y] == 4))// || (tiles[x, y] == 5) || (tiles[x,y] == 2))
                {
                    //deleteBlock(x, y);
                   // createBlock(x, y, 4);
                }
            }
        Debug.Log("final number Of Extra Tiles: " + numOfExtraTiles);
        Debug.Log("done!");

    }
    public void createCollectable(int _x, int _y, int _type)
    {
        tiles[_x, _y] = _type;
        copyTiles[_x, _y] = _type;

    }
	public void launchObjects(){
		
		for(int x=0; x < mapSizeX; x++) {
			for(int y=0; y < mapSizeX; y++) {
                if (((tiles[x, y] != 6) && (tiles[x, y] != 7)) || ((x == selectedUnit.transform.position.x) && (y == selectedUnit.transform.position.y)))
                		continue;
                CollectableObjectType tt = myCollectable;
				GameObject go = (GameObject)Instantiate( tt.tileVisualPrefab, new Vector3(x, y, -1), Quaternion.identity );

				CollectableObject ct = go.GetComponent<CollectableObject>();
				ct.tileX = x;
				ct.tileY = y;
				ct.map = this;
			}
		}
        Debug.Log("objects made");
	}

    public bool checkPossibleMove(int _x , int _y){
	    switch(tiles[_x , _y]){
		    case 0:
                return true;
		    case 1: return false;
		    case 2: return false;
		    case 3: return true;
		    default: return false;
	        }
	    }
    gameState getState()
    {
        gameState currentState = new gameState();
        currentState.tiles = tiles;
        currentState.setPlayerPositionX(selectedUnit.GetComponent<Unit>().tileX);
        currentState.setPlayerPositionY(selectedUnit.GetComponent<Unit>().tileY);
        currentState.setScore(0); ///HERE WE NEED TO PUT IN THE SCORING
        return currentState;
    }
    void loadState(gameState _ls)
    {
        tiles = _ls.tiles;
        // = _ls.getScore(); We need to get score from here;
        selectedUnit.GetComponent<Unit>().tileX = _ls.getPlayerPositionX();
        selectedUnit.GetComponent<Unit>().tileY = _ls.getPlayerPositionY();
    }
    //bool deathDecider();
    //bool blockDecider();
    
    bool deathDecider(Genome _playerGenome, int _x, int _y)
    {
        if (tiles[_x,_y] == 1)
        {
            if(_playerGenome.getAvoidedDeath() >= 0.5f)
            {
                return false;
            }
            else
            {
                _playerGenome.fitnessScore -= 100.0f;
                playerDied = true;
                Debug.Log("Player died ******!");
                return true;
                //kill the player and return somehting that will end the reucrsion
            }
        }
        //a death can mean a special tiles[x,y] value to stop all the recursions;

        return false;
    }

    bool blockDecider(Genome _playerGenome, int _x, int _y)
    {
        if ((copyTiles[_x, _y] == 1) && (tiles[_x,_y] == 5))//saps points
        {//maybe we need a different identifier :p
            int thresholdSteps = (int)Mathf.Floor(numOfBlockedTiles * _playerGenome.collisionWithObstacle);
            Debug.Log("Stepping On case. thresh: " + thresholdSteps + "and :" + _playerGenome.blockedSteps * 2);
            
            if ((_playerGenome.blockedSteps * 2) <  thresholdSteps) { 
                _playerGenome.blockedSteps++;
                _playerGenome.fitnessScore -= 10;
                Debug.Log("stepping on block...");
                return false;//blockdecider there is no block
            }
            else _playerGenome.blockedSteps++;//whatnow???
        }
        if ((copyTiles[_x, _y] == 0) && (tiles[_x, _y] == 5))//saps points
        {//maybe we need a different identifier :p

            int thresholdSteps = (int)Mathf.Floor(numOfExtraTiles * _playerGenome.revisitedSteps);//we see if this genome likes stepping on an extra tile
            Debug.Log("Saps points. thresh: " + thresholdSteps + "and :" + _playerGenome.extraSteps * 2);
            
            if ((_playerGenome.extraSteps * 2) < thresholdSteps)
            {
                _playerGenome.extraSteps++;
                _playerGenome.fitnessScore += 10;//worng!!!
                return false;
            }
            else _playerGenome.extraSteps++;//whatnow???
        }
        //need to check because copyTiles is mostly 1s and 0s at this point
        if (copyTiles[_x, _y] == 6)//saps points but give points as well wrong!!!
        {//maybe we need a different identifier :p
            int thresholdSteps = (int)Mathf.Floor(numOfBlockedTiles * _playerGenome.collisionWithObstacle);
            Debug.Log("Saps points but give them case. thresh: " + thresholdSteps + "and :" + _playerGenome.blockedSteps * 2);
            
            if ((_playerGenome.blockedSteps * 2) < thresholdSteps)
            {
                _playerGenome.blockedSteps++;
                return false;
            }
            else _playerGenome.blockedSteps++;//whatnow???
        }
        if (copyTiles[_x, _y] == 7)//gives points
        {//maybe we need a different identifier :p
            int thresholdSteps = (int)Mathf.Floor(numOfExtraTiles * _playerGenome.revisitedSteps);//we see if this genome likes stepping on an extra tile
            Debug.Log("gives points case. thresh: " + thresholdSteps + "and :" + _playerGenome.extraSteps * 2);
            
            if ((_playerGenome.extraSteps * 2) < thresholdSteps)
            {
                _playerGenome.extraSteps++;
                _playerGenome.fitnessScore += 10;
                return true;
            }
            else _playerGenome.extraSteps++;
        }
        return false;
    }
    //void applyMove(int _x, int _y);
    bool solver(int _x , int _y)
    {
        solverSteps++;
        int extraCover = 5;
        if (tiles[_x, _y] == 3)
            return true;
        if (tiles[_x, _y] == 1) {
            //if (deathDecider) { 
            //    applyMove(_x, _y);
                //return something to kill recursion here    
            //}
            return false;
        }
        if (tiles[_x, _y] == 2)
        {
            extraCover = 2;
        }
        if (tiles[_x, _y] == 4) { //backtracing
            return false;
        }
        copyTiles[_x, _y] = tiles[_x,_y];///juggad
        tiles[_x, _y] = 4;
        bool result;
        result = solver(_x + 1, _y);
        if (result) { return true; }

        result = solver(_x, _y + 1);
        if (result) { return true; }

        // Try to go Left
        result = solver(_x - 1, _y);
        if (result) { return true; }

        // Try to go Down
        result = solver(_x ,_y - 1);
        if (result) { return true; }
        tiles[_x, _y] = extraCover;
        return false;
    }
    bool solver3(int _x, int _y)//expecting values 1 or 0. 3 is 3.... 4 and 5 are zeros.
    {
        solverSteps2++;
        Debug.Log("For solverSteps2: " + solverSteps2);
        Debug.Log("copytiles " + _x + "," + _y + ": " + copyTiles[_x, _y]);
        Debug.Log("tiles " + _x + "," + _y + ": " + tiles[_x, _y]);
        if (tiles[_x, _y] == 3) { 
           Debug.Log("here i flipped -1");
           return true;
        }
        if (copyTiles[_x, _y] == 1)
        {
            Debug.Log("deathDecider: " + deathDecider(genomes[0], _x, _y));
            if (deathDecider(genomes[0] , _x , _y)) {
                return true;
                //    applyMove(_x, _y);
            //return something to kill recursion here    
            }
            if (tiles[_x, _y] == 5)
            {
                Debug.Log("blockDecider: " + blockDecider(genomes[0], _x, _y));
                if (!blockDecider(genomes[0], _x, _y))//blockdecider says don't move ahead
                {
                    Debug.Log("lalala");

                }
                else return false;
            }
            else return false;
        }
        if (((tiles[_x, _y] == 2) || (tiles[_x, _y] == 6)))// && !deathDecider(genomes[0], _x, _y))// || (tiles[_x, _y] == 7))//when copyTiles is 0 check what the tile is. if tile is 7 let it go unchecked :?
        {
            if (!blockDecider(genomes[0], _x, _y))
            {
                Debug.Log("here i flipped 0");
            }   else return false;
            
            //    applyMove(_x, _y);
        }
        if ((tiles[_x, _y] == 5 )&&( copyTiles[_x,_y] == 0))
        { //backtracing
            if (!blockDecider(genomes[0], _x, _y))
            {
                Debug.Log("here i flipped 1");
            }
            else return false;
                
        }
        if (copyTiles[_x, _y] == 4)
        { //backtracing
            return false;
        }
        copyTiles[_x, _y] = 4;
        //tiles[_x, _y] = 4;
        bool result;
        result = solver3(_x + 1, _y);
        if (result) { Debug.Log("here i flipped 2"); return true; }

        result = solver3(_x, _y + 1);
        if (result) { Debug.Log("here i flipped 3"); return true; }

        // Try to go Left
        result = solver3(_x - 1, _y);
        if (result) { Debug.Log("here i flipped 4"); return true; }

        // Try to go Down
        result = solver3(_x, _y - 1);
        if (result) { Debug.Log("here i flipped 5"); return true; }
        copyTiles[_x, _y] = 8;
        //tiles[_x, _y] = 8;
        Debug.Log("here i flipped 6");
        return false;
    }
    void moveLogic()
    {

        for (int x = 0; x < mapSizeX; x++)
        {
            for (int y = 0; y < mapSizeX; y++)
            {
                if (copyTiles[x, y] == 3)
                    continue;
                if ((tiles[x, y] == 4) || (tiles[x, y] == 5)|| (tiles[x, y] == 0) || (tiles[x, y] == 7))
                { //leaving 2 or 6 to be deadly- they are both blocks that could have been in answer.
                    if ((copyTiles[x,y] == 7) ||(copyTiles[x,y] == 0)) { //let open collectibles be a valid path. thsi why I cannot have more than one open square at any time. four-way block situation
                        copyTiles[x, y] = 0;
                    }
                    else { copyTiles[x, y] = 1; }
                    Debug.Log("CopyTile " + x + "," + y + " is: "+ copyTiles[x,y] );
                    
                }
                else
                {
                    copyTiles[x, y] = 1;
                }
            }
        }
        copyTiles[playerPosX, playerPosY] = 0;
        Debug.Log("calling solver2..." + solverSteps2);
        Debug.Log("with playerPosX:" +playerPosX +"," + playerPosY);
        if (solver3(playerPosX, playerPosY))
            Debug.Log("sovler 2 solved it");
        else
            Debug.Log("not solved by solver 2..");
        for (int x = 0; x < mapSizeX; x++)
            for (int y = 0; y < mapSizeY; y++)
            {
                if ((copyTiles[x, y] == 4 ) )//|| (copyTiles[x, y] == 8))
                {
                    deleteBlock(x, y);
                    createBlock(x, y, 4);
                }
                if (copyTiles[x, y] == 1)
                {
                    //deleteBlock(x, y);
                    //createBlock(x, y, 1);
                }
            }
        Debug.Log("done! and solverSteps2: " + solverSteps2);

    }
    bool solver2(int _x , int _y) { 
        solverSteps2++;
        if (copyTiles[_x, _y] == 3)
            return true;
        if (copyTiles[_x, _y] == 1)
        {
            //if (deathDecider) { 
            //    applyMove(_x, _y);
            //return something to kill recursion here    
            //}
            return false;
        }
        if (copyTiles[_x, _y] == 4)
        { //backtracing
            //call a valid graphic move
            return false;
        }

        copyTiles[_x, _y] = 4;
        bool result;
        result = solver(_x + 1, _y);
        if (result) { return true; }

        result = solver(_x, _y + 1);
        if (result) { return true; }

        // Try to go Left
        result = solver(_x - 1, _y);
        if (result) { return true; }

        // Try to go Down
        result = solver(_x, _y - 1);
        if (result) { return true; }
        copyTiles[_x, _y] = 5;
        return false;
        }
    private Genome getRandomGenome()
    {
        return genomes[Random.Range(0, genomes.Count)];
    }
    private Genome makeChild(Genome _mom, Genome _dad)
    {
        Genome newChild = new Genome();

        float[] _ad = { _mom.getAvoidedDeath(), _dad.getAvoidedDeath() };
        newChild.setAvoidedDeath(_ad[Random.Range(0, 2)]);

        float[] _ct = { _mom.getCoinsTaken(), _dad.getCoinsTaken() };
        newChild.setCoinsTaken(_ct[Random.Range(0, 2)]);

        float[] _cwo = { _mom.getCollisionWithObstacle(), _dad.getCollisionWithObstacle() };
        newChild.setCollisionWithObstacle(_cwo[Random.Range(0, 2)]);

        float[] _rs = { _mom.getRevisitedSteps(), _dad.getRevisitedSteps() };
        newChild.setRevisitedSteps(_rs[Random.Range(0, 2)]);

        float[] _tm = { _mom.getTotalMovement(), _dad.getTotalMovement() };
        newChild.setTotalMovement(_tm[Random.Range(0, 2)]);

        //fitness needs to be set
        return newChild;
    }
    void initialize()
    {//basically unity start
        Record initialRecord = new Record();
        initialRecord.populationSize = populationSize;
        //any game functions
        //saveState = getState();
        //roundState = getState(); returns values of the genome

        createInitialPopulation(); //creates populationSize number of genomes with random values of genome parameters

    }
    void createInitialPopulation()
    {
        for (int i = 0; i < populationSize; i++)
        {
            Genome singleGenome = new Genome();
            genomes.Add(singleGenome);
        }
        evaluateNextGenome();
    }
    void evaluateNextGenome()
    {
        currentGenome++;
        if (currentGenome == genomes.Count)
            evolve();
        //load current gameState ****************
        movesTaken = 0;
        //makeNextMove(); ***********
    }
    void evolve()
    {
        currentGenome = 0;
        generation++;
        //Reset the game *********
        //roundstate = getState();
        //sort the genomes list based on fitness value. fitness is the score in the game (yet) ****
        Record addToArchive = new Record();
        addToArchive.populationSize = populationSize;
        addToArchive.currentGeneration = generation;
        addToArchive.setElite(genomes[0]);
        //the genome from record is missing here.
        if (genomes.Count > populationSize / 2)
        {
            genomes.RemoveRange(populationSize / 2, genomes.Count);
        }
        List<Genome> children = new List<Genome>();
        //add the fittest genome to array
        children.Add(genomes[0]);
        //add population sized amount of children
        while (children.Count < populationSize)
        {
            //crossover between two random genomes to make a child
            children.Add(makeChild(getRandomGenome(), getRandomGenome()));
        }
        genomes.Clear();
        for (int i = 0; i < children.Count; i++)
        {
            genomes.Add(children[i]);
        }
        addToArchive.setGenome(genomes);
        archive.Add(addToArchive);

    }

    void makeNextMove()
    {
        movesTaken++;
        //I think the algorithm should be created a new here.
        if (movesTaken >= moveLimits)
        {
            //store the genome score. ********
            evaluateNextGenome();
        }
        else
        {
            //call the tile map functions....the makenextmove function can be called in the update of the game.
        }
    }
}
public class Genome
{
    public int id;
    public float coinsTaken;
    public float totalMovement;
    public float collisionWithObstacle;
    public float avoidedDeath;
    public float revisitedSteps;
    public float fitnessScore;
    public int extraSteps; // to store how many extra Steps the user can still take. should be an even number.
    public int blockedSteps; // to store how many blocked Steps the user can still take. should be an even number.
    public Genome()
    {
        coinsTaken = Random.Range(0.0f, 1.0f) - 0.5f;
        totalMovement = Random.Range(0.0f, 1.0f) - 0.5f;
        collisionWithObstacle = Random.Range(0.0f, 1.0f) - 0.5f;
        avoidedDeath = Random.Range(0.0f, 1.0f);
        revisitedSteps = Random.Range(0.0f, 1.0f) - 0.5f;
        fitnessScore = 0.0f;
    }//distance from end title
     //coins leftToEat
     //set thresholds for these genome sets. favor some moves in the recursive sessions. die frequently
     //thresholds will be used to identify blocks in whole map once or at each deicison step etc. 

    public void setCoinsTaken(float _ct) { coinsTaken = _ct; }
    public void setTotalMovement(float _tm) { totalMovement = _tm; }
    public void setCollisionWithObstacle(float _cwo) { collisionWithObstacle = _cwo; }
    public void setAvoidedDeath(float _ad) { avoidedDeath = _ad; }
    public void setRevisitedSteps(float _rs) { revisitedSteps = _rs; }

    public float getCoinsTaken() { return coinsTaken; }
    public float getTotalMovement() { return totalMovement; }
    public float getCollisionWithObstacle() { return collisionWithObstacle; }
    public float getAvoidedDeath() { return avoidedDeath; }
    public float getRevisitedSteps() { return revisitedSteps; }
}

public class Record
{
    public int populationSize;
    public int currentGeneration;
    List<Genome> genome; //hmmmm
    Genome elites;

    public Record()
    {
        populationSize = 0;
        currentGeneration = 0;
    }
    public void setElite(Genome _elite) { elites = _elite; }
    public void setGenome(List<Genome> _g) { genome.Clear(); genome = _g; }
    public Record(int _pS, int _currentGeneration)
    {
        populationSize = _pS;
        currentGeneration = _currentGeneration;
    }
    //Other properties, methods, events...
}
public class gameState
{
    public int[,] tiles;
    int playerPositionX;
    int playerPositionY;
    int score;

    public gameState()
    {
        
    }

    public void setPlayerPositionX(int _t) { playerPositionX = _t; }
    public void setPlayerPositionY(int _t) {playerPositionY = _t; }
    public void setScore(int _t) { score = _t; }

    public int getPlayerPositionX() { return playerPositionX; }
    public int getPlayerPositionY() { return playerPositionY; }
    public int getScore() { return score; }
}//the maze need to make sure that somehow the current genome will take different moves in this run. 
//we know the length of solveable maze
//we know the length of extra blocks we can travel
// we know how many of those extra blocks take away more power. we can select the number of such blocks that we will travel everytime to increase the score
//we can implement our solution by generating a new maze in which our new paths are allowed and let the same solution solve that maze.