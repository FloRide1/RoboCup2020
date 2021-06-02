import processing.core.*; 
import processing.data.*; 
import processing.event.*; 
import processing.opengl.*; 

import processing.net.*; 
import krister.Ess.*; 
import org.json.*; 
import java.io.*; 
import java.io.BufferedWriter; 
import java.io.FileWriter; 
import java.io.File; 
import java.util.zip.*; 
import processing.data.JSONArray; 
import processing.data.JSONObject; 

import krister.Ess.*; 
import org.json.*; 
import org.json.zip.*; 

import java.util.HashMap; 
import java.util.ArrayList; 
import java.io.File; 
import java.io.BufferedReader; 
import java.io.PrintWriter; 
import java.io.InputStream; 
import java.io.OutputStream; 
import java.io.IOException; 

public class mslrb2015 extends PApplet {

//<>//
/* ==================================
MSL RefBox 2015 (Processing 3)
	LMFerreira
	RDias
	FAmaral 
	BCunha
================================== */




public static final String MSG_VERSION="1.5.0";
public static final String MSG_VERSION_MSG="(RoboCup 2019)";
public static final String MSG_WINDOWTITLE="RoboCup MSL RefBox 2015 - "+MSG_VERSION+" "+MSG_VERSION_MSG;
public static final String MSG_HALFTIME="End Current Part ?";
public static final String MSG_RESET="Reset Game ?";
public static final String MSG_REPAIR="How many robots for repair ?";
public static final String MSG_WAIT="Please WAIT! Compressing files.";
public static String MSG_HELP="SHORT CUT KEYS:";

public static final int appFrameRate = 25;

public static String[] Teamcmds= { "KickOff", "FreeKick", "GoalKick", "Throw In", "Corner", "Penalty", "Goal", "Repair", "Red", "Yellow" };
public static String[] Commcmds= { "START", "STOP", "DropBall", "Park", "End Part",  "RESET", "EndGame" };

public static final String[] cCTeamcmds= { "K", "F", "G", "T", "C", "P", "A", "O", "R", "Y" };
public static final String[] cMTeamcmds= { "k", "f", "g", "t", "c", "p", "a", "o", "r", "y" };
public static final int CMDID_TEAM_KICKOFF = 0;
public static final int CMDID_TEAM_FREEKICK = 1;
public static final int CMDID_TEAM_GOALKICK = 2;
public static final int CMDID_TEAM_THROWIN = 3;
public static final int CMDID_TEAM_CORNER = 4;
public static final int CMDID_TEAM_PENALTY = 5;
public static final int CMDID_TEAM_GOAL = 6;
public static final int CMDID_TEAM_REPAIR_OUT = 7;
public static final int CMDID_TEAM_REDCARD = 8;
public static final int CMDID_TEAM_YELLOWCARD = 9;

public static final String[] cCommcmds= { "s", "S", "N", "L", "h", "Z", "e" };  
public static final int CMDID_COMMON_START = 0;
public static final int CMDID_COMMON_STOP = 1;
public static final int CMDID_COMMON_DROP_BALL = 2;
public static final int CMDID_COMMON_PARKING = 3;
public static final int CMDID_COMMON_HALFTIME = 4;
public static final int CMDID_COMMON_RESET = 5;
public static final int CMDID_COMMON_ENDGAME = 6;

public static ScoreClients scoreClients = null;
public static MSLRemote mslRemote = null;
public static MyServer BaseStationServer;
public static Client connectingClient = null;

public static Team teamA,teamB;
public static Button[] bTeamAcmds = new Button[CMDID_TEAM_YELLOWCARD + 1];
public static Button[] bTeamBcmds = new Button[CMDID_TEAM_YELLOWCARD + 1];
public static Button[] bCommoncmds = new Button[CMDID_COMMON_RESET + 1];
public static BSliders[] bSlider = new BSliders[4];

public static Table teamstable;
public static TableRow teamselect;
public static long updateScoreClientslasttime=0;
public static long tstartTime=0, tsplitTime=0, tprevsplitTime=0;
public static boolean TESTMODE=false, stopsplittimer=true, VOICECOACH=false, REMOTECONTROLENABLE=false;
public static char LastKickOff='.';
public static String[] Last5cmds= { ".", ".", ".", ".", "." };
public static String LogFileName;
public static String lastaction=".";
public static String gametime = "", gameruntime = "";

//GUI
public static final int popUpButtons = 9;					// Currently defined number of Pop Up Buttons
public static Button[] bPopup = new Button[popUpButtons];	// button 0 is reserved.
public static PVector offsetLeft= new PVector(230, 180);
public static PVector offsetRight= new PVector(760, 180);
public static PFont buttonFont, clockFont, panelFont, scoreFont, debugFont, teamFont, watermark;
// public static PImage backgroundImage;
public PImage backgroundImage;
public PImage rcfLogo;

// Watches as timers
public static StopWatch mainWatch;            // Main watch allways running. Reseted @end-of-parts and end-halfs
public static StopWatch playTimeWatch;        // Actual played time. Reseted @end-of-parts and end-halfs
public static StopWatch SetPieceDelay;        // Timer for measuring set piece restart

// Sounds
public static AudioChannel soundMaxTime;
public static long lastPlayMillis = 0;

public static PApplet mainApplet = null;
public static boolean altK = false;
public static boolean forceKickoff = false;


/**************************************************************************************************************************
* This the Processing setup() function
* The setup() function is called once when the program starts.
* It's used to define initial enviroment properties such as screen size and background color and to load media
such as images and fonts as the program starts.
* There can only be one setup() function for each program and it shouldn't be called again after its initial execution.
* Note: Variables declared within setup() are not accessible within other functions, including draw().
**************************************************************************************************************************/
public void setup() {
	mainApplet = this;

	backgroundImage = loadImage("img/bg_normal.png");
	

	surface.setTitle(MSG_WINDOWTITLE); 
	clockFont = createFont("fonts/LCDM.TTF", 64, false);
	scoreFont = createFont("fonts/LED.ttf", 40, false);
	buttonFont=loadFont("fonts/Futura-CondensedExtraBold-24.vlw");
	teamFont=loadFont("fonts/Futura-CondensedExtraBold-52.vlw");
	panelFont=loadFont("fonts/Futura-CondensedExtraBold-20.vlw");
	debugFont=loadFont("fonts/Monaco-14.vlw");
	watermark=createFont("Arial", 112, false);

	createDir(mainApplet.dataPath("tmp/"));
	createDir(mainApplet.dataPath("logs/"));

	//==============================================
	//=== Modules Initialization
	Config.Load(this, "config.json");                                     // Load config file
	Log.init(this);                                                       // Init Log module
	comms_initDescriptionDictionary();                                    // Initializes the dictionary for communications with the basestations 

	scoreClients = new ScoreClients(this);        // Load score clients server
	BaseStationServer = new MyServer(this, Config.basestationServerPort); // Load basestations server
	mslRemote = new MSLRemote(this, Config.remoteServerPort);             // Load module for MSL remote control

	teamA = new Team(Config.defaultCyanTeamColor,true);                   // Initialize Cyan team (Team A)
	teamB = new Team(Config.defaultMagentaTeamColor,false);               // Initialize Magenta team (Team B)
	teamstable = new TeamTableBuilder("msl_teams.json").build();          // Load teams table

	//==============================================
	//=== GUI Initialization
	initGui();
	RefreshButonStatus1();

	mainWatch = new StopWatch(false, 0, true, false);
	mainWatch.resetStopWatch();
	mainWatch.startSW();

	playTimeWatch = new StopWatch(false, 0, false, false);
	playTimeWatch.resetStopWatch();
	playTimeWatch.startSW();

	SetPieceDelay = new StopWatch(true, 0, true, false);
	
	frameRate(appFrameRate);

	MSG_HELP += "\nSpaceTab > Force STOP action";
	MSG_HELP += "\nAlt + K    > Enable KickOff buttons";
	MSG_HELP += "\nAlt + R    > Force RESET of game at any time";
	MSG_HELP += "\nESC        > Exit PopUp window";
	MSG_HELP += "\nH           > Show this pop up";
	// Sounds initialization
	Ess.start(this); // start up Ess
	if(Config.sounds_maxTime.length() > 0) {
		soundMaxTime = new AudioChannel(dataPath("sounds/" + Config.sounds_maxTime));
	}else{
		soundMaxTime = null;
	}
}

/**************************************************************************************************************************
This the Processing draw() function 
Called directly after setup(), the draw() function continuously executes the lines of code contained inside its block
until the program is stopped or noLoop() is called. draw() is called automatically and should never be called explicitly.
It should always be controlled with noLoop(), redraw() and loop(). If noLoop() is used to stop the code in draw() from executing, 
then redraw() will cause the code inside draw() to be executed a single time, and loop() will cause the code inside draw() 
to resume executing continuously.
The number of times draw() executes in each second may be controlled with the frameRate() function
It is common to call background() near the beginning of the draw() loop to clear the contents of the window, as shown in the first 
example above. Since pixels drawn to the window are cumulative, omitting background() may result in unintended results, especially 
when drawing anti-aliased shapes or text.
There can only be one draw() function for each sketch, and draw() must exist if you want the code to run continuously, or to process 
events such as mousePressed(). Sometimes, you might have an empty call to draw() in your program, as shown in the second example above.  
**************************************************************************************************************************/
public void draw() {

	background(backgroundImage);

	// Update Timers and Watches
	mainWatch.updateStopWatch();
	playTimeWatch.updateStopWatch();
	SetPieceDelay.updateStopWatch();

	long t1 = mainWatch.getTimeSec();
	long t2 = playTimeWatch.getTimeSec();

	gametime = nf(PApplet.parseInt(t1/60), 2)+":"+nf(PApplet.parseInt(t1%60), 2);
	gameruntime = nf(PApplet.parseInt(t2/60), 2)+":"+nf(PApplet.parseInt(t2%60), 2);

	//update basestations data   
	long t=System.currentTimeMillis();
	if ( (t-updateScoreClientslasttime) >= Config.scoreClientsUpdatePeriod_ms ) scoreClients.update_tTeams(gametime,gameruntime);

	//verifyremotecontrol();
	mslRemote.checkMessages();
	checkBasestationsMessages();

	for (int i = 0; i < bCommoncmds.length; i++)
	bCommoncmds[i].update();

	for (int i = 0; i < bTeamAcmds.length; i++) {
		bTeamAcmds[i].update();
		bTeamBcmds[i].update();
	}

	teamA.updateUI();
	teamB.updateUI();

	for (int i = 0; i < bSlider.length; i++)
		bSlider[i].update();				

	StateMachineCheck(); // Check scheduled state change
	RefreshButonStatus1(); // Refresh buttons

	fill(255);
	textAlign(CENTER, CENTER);

	//dispay score
	textFont(scoreFont);
	text("[  "+teamA.Score+"  -  "+teamB.Score+"  ]", 500, 25);

	//display main running clock
	textFont(clockFont);
	fill(255);
	text( gametime, 500, 85);

	//display effective game clock  
	textFont(panelFont);
	text(StateMachine.GetCurrentGameStateString()+" ["+gameruntime+"]", 500, 140);

	//debug msgs  
	textFont(debugFont);
	textAlign(LEFT, BOTTOM);
	fill(0xff00ff00);
	for (int i=0; i<5; i++)	{
		text( Last5cmds[i], 340, height-4-i*18);
		fill(0xff007700);
	}
	fill(255);
	textAlign(CENTER, BOTTOM);
	text("Press H for a short help!", 500, 530);

	//server info
	textAlign(CENTER, BOTTOM);

	text(scoreClients.clientCount()+" score clients :: "+BaseStationServer.clientCount+" basestations", width/2, 578);  

	//==========================================

	if (Popup.isEnabled()) {
		Popup.draw();
	}

	//==========================================

	if(SetPieceDelay.getStatus() && SetPieceDelay.getTimeMs() == 0)
	{
		SetPieceDelay.stopTimer();
		soundMaxTime.cue(0);
		soundMaxTime.play();
	}
}

/**************************************************************************************************************************
*   This the Processing exit() function 
* Quits/stops/exits the program. Programs without a draw() function exit automatically after the last line has run, but programs 
* with draw() run continuously until the program is manually stopped or exit() is run.
* Rather than terminating immediately, exit() will cause the sketch to exit after draw() has completed (or after setup() 
* completes if called during the setup() function).
* For Java programmers, this is not the same as System.exit(). Further, System.exit() should not be used because closing 
* out an application while draw() is running may cause a crash (particularly with P3D). 
/**************************************************************************************************************************/
public void exit() {
	println("Program is stopped !!!");

	// Reset teams to close log files
	if(teamA != null) teamA.reset();
	if(teamB != null) teamB.reset();

	LogMerger merger = new LogMerger(Log.getTimedName());
	merger.zipAllFiles();

	// Stop all servers
	scoreClients.stopServer();
	BaseStationServer.stop();
	mslRemote.stopServer();

	super.exit();
}

public void initGui()
{
	//common commands
	for (int i=0; i < bCommoncmds.length; i++){
		bCommoncmds[i] = new Button(435+130*(i%2), 275+i*35-35*(i%2), Commcmds[i], 0xffC0C000, -1, 255, 0xffC0C000);

		// End part and reset need confirmation popup (don't send message right away)
		if(i <= CMDID_COMMON_PARKING) {
			bCommoncmds[i].cmd = "" + cCommcmds[i];
			bCommoncmds[i].msg = "" + Commcmds[i];
		}
	}
	bCommoncmds[0].setcolor(0xff12FF03, -1, -1, 0xff12FF03);  //Start  / green
	bCommoncmds[1].setcolor(0xffE03020, -1, -1, 0xffE03030);  //Stop  /red  #FC0303 

	for (int i=0; i<6; i++) {
		bTeamAcmds[i] = new Button(offsetLeft.x, offsetLeft.y+70*i, Teamcmds[i], 255, -1, 255, Config.defaultCyanTeamColor);
		bTeamAcmds[i].cmd = "" + cCTeamcmds[i];
		bTeamAcmds[i].msg = Teamcmds[i];

		bTeamBcmds[i] = new Button(offsetRight.x, offsetRight.y+70*i, Teamcmds[i], 255, -1, 255, Config.defaultMagentaTeamColor);
		bTeamBcmds[i].cmd = "" + cMTeamcmds[i];
		bTeamBcmds[i].msg = Teamcmds[i];
	}

	bTeamAcmds[6] = new Button(offsetLeft.x-135, offsetLeft.y, Teamcmds[6], Config.defaultCyanTeamColor, -1, 255, Config.defaultCyanTeamColor);   // Goal A
	bTeamAcmds[7] = new Button(offsetLeft.x-135, offsetLeft.y+70*4, Teamcmds[7], Config.defaultCyanTeamColor, -1, 255, Config.defaultCyanTeamColor); // Repair A
	bTeamAcmds[8] = new Button(offsetLeft.x-162, offsetLeft.y+70*5, "", 0xffFC0303, 0xff810303, 255, 0xffFC0303);  //red card A
	bTeamAcmds[9] = new Button(offsetLeft.x-105, offsetLeft.y+70*5, "", 0xffFEFF00, 0xff808100, 255, 0xffFEFF00);  //yellow card A

	bTeamBcmds[6] = new Button(offsetRight.x+135, offsetRight.y, Teamcmds[6], Config.defaultMagentaTeamColor, -1, 255, Config.defaultMagentaTeamColor);  //Goal B
	bTeamBcmds[7] = new Button(offsetRight.x+135, offsetRight.y+70*4, Teamcmds[7], Config.defaultMagentaTeamColor, -1, 255, Config.defaultMagentaTeamColor);//Repair B
	bTeamBcmds[8] = new Button(offsetRight.x+162, offsetRight.y+70*5, "", 0xffFC0303, 0xff810303, 255, 0xffFC0303);  //red card B
	bTeamBcmds[9] = new Button(offsetRight.x+105, offsetRight.y+70*5, "", 0xffFEFF00, 0xff808100, 255, 0xffFEFF00);  //yellow card B

	for (int i = 6; i < 10; i++) {
		bTeamAcmds[i].cmd = "" + cCTeamcmds[i];
		bTeamAcmds[i].msg = Teamcmds[i];
		bTeamBcmds[i].cmd = "" + cMTeamcmds[i];
		bTeamBcmds[i].msg = Teamcmds[i];
	}

	// OFF-state goal button (subtract goal)
	bTeamAcmds[6].msg_off = "Goal-";
	bTeamAcmds[6].cmd_off = "" + COMM_SUBGOAL_CYAN;
	bTeamBcmds[6].msg_off = "Goal-";
	bTeamBcmds[6].cmd_off = "" + COMM_SUBGOAL_MAGENTA;


	bTeamAcmds[8].setdim(32, 48); 
	bTeamAcmds[9].setdim(32, 48); 
	bTeamBcmds[8].setdim(32, 48);  //red C resize
	bTeamBcmds[9].setdim(32, 48);  //yellow C resize

	bPopup[0] = new Button(0, 0, "", 0, 0, 0, 0);
	bPopup[1] = new Button(0, 0, "yes", 220, 0xff129003, 0, 0xff129003);
	bPopup[2] = new Button(0, 0, "no", 220, 0xffD03030, 0, 0xffD03030);//
	bPopup[3] = new Button(0, 0, "cyan", 220, Config.defaultCyanTeamColor, 0, 0xff804035);
	bPopup[4] = new Button(0, 0, "magenta", 220, Config.defaultMagentaTeamColor, 0, Config.defaultMagentaTeamColor);
	bPopup[5] = new Button(0, 0, "1", 220, 0xff6D9C75, 0, 0xff6D9C75); bPopup[5].setdim(80, 48);
	bPopup[6] = new Button(0, 0, "2", 220, 0xff6D9C75, 0, 0xff6D9C75); bPopup[6].setdim(80, 48);
	bPopup[7] = new Button(0, 0, "3", 220, 0xff6D9C75, 0, 0xff6D9C75); bPopup[7].setdim(80, 48);
	bPopup[8] = new Button(0, 0, "OK", 220, 0xff6D9C75, 0, 0xff6D9C75); bPopup[8].setdim(80, 48);
	for (int n = 0; n < popUpButtons; n++)
	bPopup[n].disable();

	bSlider[0]=new BSliders("Testmode",420,460,true, TESTMODE);
	bSlider[1]=new BSliders("Log",420+132,460,true, Log.enable);
	bSlider[2]=new BSliders("Remote",420,460+32,Config.remoteControlEnable, REMOTECONTROLENABLE);
	bSlider[3]=new BSliders("Coach",420+132,460+32,false, VOICECOACH);

	textFont(debugFont);
	fill(0xffffffff);
	textAlign(CENTER, BOTTOM);
	text("Press H for a short help!", 500, 460+60);
	
	buttonCSTOPactivate();
}

public boolean createDir(String dirPath)
{
	// Create logs directory if necessary
	File logsDir = new File(dirPath);
	if(!logsDir.exists() || !logsDir.isDirectory())
	{
		if(!logsDir.mkdir()){
			println("ERROR - Could not create logs directory.");
			return false;
		}
	}
	return true;
}
class BSliders {
	String Label;
	boolean enabled;
	Boolean on;
	float posx; 
	float posy;
	int c; 

	BSliders(String Label, float x, float y, boolean enable, boolean on ) { 
		this.Label=Label;
		this.posx=x;
		this.posy=y;
		this.on=on;
		this.enabled=enable;
		this.c=255;
	}

	public void update() {
		textAlign(LEFT, BOTTOM);
		rectMode(CENTER);
		strokeWeight(1);
		if (enabled) c=192;
		else c=92;
		stroke(c); noFill(); 
		rect(posx, posy, 48, 23, 12);
		fill(c); noStroke();
		textFont(debugFont);
		if (on) {
			rect(posx-8+17, posy, 26, 17, 12);//on
			fill(92);text("on", posx+2, posy+7);
		}
		else {
			rect(posx-8, posy, 26, 17, 12);//off
			fill(92); text("off", posx-19, posy+7);
		}
		fill(c);
		text(Label, posx+30, posy+7);
	}


	public boolean mouseover() {
		if ( mouseX>(posx-24-2) && mouseX<(posx+24+2) && mouseY>(posy-12-2) && mouseY<(posy+12+2) ) return true;
		return false;
	}

	public void toogle() {
		if (this.enabled) this.on=!on;  
	}

	public void enable() {
		this.enabled=true;
	}
	public void disable() {
		this.enabled=false;
	}
}

public void setbooleansfrombsliders() {
	TESTMODE=bSlider[0].on;
	Log.enable = bSlider[1].on;
	REMOTECONTROLENABLE=bSlider[2].on;
	VOICECOACH=bSlider[3].on; 
}
class Button {
	float x; 
	float y;
	String bStatus;  // normal, active, disabled
	Boolean HOVER;
	String Label;
	int bwidth=116; 
	int bheight=48;
	int hbwidth=bwidth/2; 
	int hbheight=bheight/2;
	int ccm = 0;
	int cstroke, cfill, cstrokeactive, cfillactive;

	public String msg = null; // long name for the command
	public String msg_off = null;
	public String cmd = null; // command (usually a char)
	public String cmd_off = null;

	// c1 > stroke color (-1 > no stroke)
	// c2 > fill collor (-1 > no fill)
	// c3 > stroke color when active (-1 > no stroke)
	// c4 > fill collor when active (-1 > no fill)
	Button(float x, float y, String Label, int c1, int c2, int c3, int c4) { 
		this.x=x;
		this.y=y;
		this.Label=Label;
		this.bStatus="disabled";
		this.HOVER=false;
		this.cstroke=c1;
		this.cfill=c2;
		this.cstrokeactive=c3;
		this.cfillactive=c4;
	}

	public void update() {
		rectMode(CENTER);
		textAlign(CENTER, CENTER);
		textFont(buttonFont);
		strokeWeight(2);

		int offset = 4;
		int cround = 8;
		if (this.isEnabled()) {
			if (this.isActive()) {
				noStroke();
				if (HOVER && cfillactive != -1) {
					fill(cfillactive, 100);
					rect(x+offset, y+offset, bwidth, bheight, cround);
				}
				if (cfillactive==-1) noFill(); 
				else fill(cfillactive);

			} else {  //not active, no hover
				if (HOVER && cfill != -1) {
					noStroke();
					if (cstroke!= -1) {
						offset += 3;  
						cround += 2;
					}
					fill(cfill, 130);
					rect(x+offset, y+offset, bwidth, bheight, cround);
				}

				if (cstroke==-1) noStroke(); 
				else stroke(cstroke);

				if (cfill==-1) noFill(); 
				else fill(cfill);
			}	
		} else { //disabled
			fill(0, 8);
			stroke(96);
		} 
		rect(x, y, bwidth, bheight, 8);
		if (HOVER) {
			ccm++;
		}

		//  Text

		if (this.isEnabled()) {
			if (this.isActive()) {
				if (cstrokeactive == -1) fill(255); 
				else fill(cstrokeactive);
			} 
			else {  //not active, no hover
				if (HOVER && cstroke != -1 && cfill == -1) {
					fill(cstroke, 100);
					text(Label, x+4, y+2);			
				}
				if (cstroke==-1) noFill(); 
				else fill(cstroke);
			}
		} else fill(96); //disabled  

		text(Label, x, y-2);//-4  , y-2
	}

	public void checkhover() {
		if ( mouseX>(x-hbwidth-2) && mouseX<(x+hbwidth+2) && mouseY>(y-hbheight-2) && mouseY<(y+hbheight+2) ) this.HOVER=true;
		else this.HOVER=false;
	}

	public boolean isDisabled() {
		if (bStatus.equals("disabled")) return true;
		else return false;
	}

	public boolean isEnabled() {
		if (bStatus.equals("disabled")) return false;
		else return true;
	}

	public boolean isActive() {
		if ( this.bStatus.equals("active") ) return true;
		else return false;
	}

	public void activate() {
		this.bStatus="active";
	}

	public void enable() {
		this.bStatus="normal";
	}

	public void disable() {
		this.bStatus="disabled";
		this.HOVER=false;
	}

	public void toggle() {
		if (this.isEnabled()) {
			if ( this.isActive() ){
				this.bStatus="normal";
				if(StateMachine.setpiece && this.Label == Teamcmds[6]) {
					StateMachine.ResetSetpiece();
					send_to_basestation(cCommcmds[1]);
				}
			}
			else this.bStatus="active";
		}
	}


	public void setcolor(int c1, int c2, int c3, int c4) {
		this.cstroke=c1;
		this.cfill=c2;
		this.cstrokeactive=c3;
		this.cfillactive=c4;
	}

	public void setdim(int w, int h) {
		bwidth=w; 
		bheight=h;
		hbwidth=bwidth/2; 
		hbheight=bheight/2;
	}

	public void setxy(float x, float y){    
		this.x=x;
		this.y=y;
	}

}

//***********************************************************************
//
public static Button buttonFromEnum(ButtonsEnum btn)
{
	if(btn.getValue() <= ButtonsEnum.BTN_RESET.getValue())
	return bCommoncmds[btn.getValue()];

	if(btn.getValue() <= ButtonsEnum.BTN_C_YELLOW.getValue())
	return bTeamAcmds[btn.getValue() - ButtonsEnum.BTN_C_KICKOFF.getValue()];

	if(btn.getValue() <= ButtonsEnum.BTN_M_YELLOW.getValue())
	return bTeamBcmds[btn.getValue() - ButtonsEnum.BTN_M_KICKOFF.getValue()];

	return null;
}

//***********************************************************************
//
public void buttonEvent(char group, int pos) {

	ButtonsEnum clickedButton = null;
	Button clickBtn = null;

	if (group=='C')
	{
		clickedButton = ButtonsEnum.items[pos];
		clickBtn = buttonFromEnum(clickedButton);
		if(clickBtn.isEnabled())
		clickBtn.toggle();
		else
		clickedButton = null;
	}
	else if (group=='A')
	{
		clickedButton = ButtonsEnum.items[pos + ButtonsEnum.BTN_C_KICKOFF.getValue()];
		clickBtn = buttonFromEnum(clickedButton);
		if(clickBtn.isEnabled())
		clickBtn.toggle();
		else
		clickedButton = null;
	}
	else if (group=='B')
	{
		clickedButton = ButtonsEnum.items[pos + ButtonsEnum.BTN_M_KICKOFF.getValue()];
		clickBtn = buttonFromEnum(clickedButton);
		if(clickBtn.isEnabled())
		clickBtn.toggle();
		else
		clickedButton = null;
	}

	if(clickedButton != null)        // A button has been clicked
	{
		boolean btnOn = buttonFromEnum(clickedButton).isActive();
		
		StateMachine.Update(clickedButton, btnOn);
		
		if(soundMaxTime != null && clickedButton.isStart()) {
			SetPieceDelay.startTimer(Config.setPieceMaxTime_ms);
			println ("Millis: " + Config.setPieceMaxTime_ms); 
		}
		
		// Special cases, that send only event message on game change (flags)
		if( clickedButton.isYellow() || clickedButton.isRed() || clickedButton.isRepair() )
		{
			// Do literally nothing...
		}else{
			if(clickedButton.isCommon())
			{
				event_message_v2(clickedButton, true);
			}else{
				event_message_v2(clickedButton, buttonFromEnum(clickedButton).isActive());
			}
		}
	}
}
// New accepted connections
public static void serverEvent(MyServer whichServer, Client whichClient) {
	try {
		if (whichServer.equals(BaseStationServer)) {
			Log.logMessage("New BaseStation @ "+whichClient.ip());
		}
		else if (mslRemote != null && mslRemote.server != null && whichServer != null && whichServer.equals(mslRemote.server)) {
			Log.logMessage("New Remote @ " + whichClient.ip());
		}
	}catch(Exception e){}
}

// Client authentication
public static void clientValidation(MyServer whichServer, Client whichClient) {
	try{
		// BASESTATION CLIENTS AUTH
		if (whichServer.equals(BaseStationServer)) {
			if (!Popup.isEnabled()) {
				if(setteamfromip(whichClient.ip()))
				connectingClient = whichClient; // Accept client!
				else
				{
					// Invalid team
					Log.logMessage("Invalid team " + whichClient.ip());
					whichClient.write(COMM_RESET);
					whichClient.stop();
				}
			} else {
				Log.logMessage("ERR Another team connecting");
				whichClient.write(COMM_RESET);
				whichClient.stop();
			}
		}
		// REMOTE CLIENTS AUTH
		else if (mslRemote != null && mslRemote.server != null && whichServer.equals(mslRemote.server)) {
			
		}
	}catch(Exception e){}
}


public static void send_to_basestation(String c){
	println("Command "+c+" :"+Description.get(c+""));
	BaseStationServer.write(c);

	//  if(!c.equals("" + COMM_WORLD_STATE))
	//  {
	Log.logactions(c);
	mslRemote.setLastCommand(c);      // Update MSL remote module with last command sent to basestations
	//  }
}

public static void event_message_v2(ButtonsEnum btn, boolean on)
{
	String cmd = buttonFromEnum(btn).cmd;
	String msg = buttonFromEnum(btn).msg;
	if(!on)
	{
		cmd = buttonFromEnum(btn).cmd_off;
		msg = buttonFromEnum(btn).msg_off;
	}

	Team t = null;
	if(btn.isCyan()) t = teamA;
	if(btn.isMagenta()) t = teamB;

	if(cmd != null && msg != null)
	{
		send_event_v2(cmd, msg, t);
	}
	println("Command: " + cmd);
}

public static void send_event_v2(String cmd, String msg, Team t)
{
	String teamName = (t != null) ? t.longName : "";
	send_to_basestation(cmd);
	scoreClients.update_tEvent(cmd, msg, teamName);
	mslRemote.update_tEvent(cmd, msg, t);
}

public void event_message(char team, boolean on, int pos) {
	if (on) {  //send to basestations
		if (team=='C' && pos<4){
			send_to_basestation(cCommcmds[pos]);
			scoreClients.update_tEvent("" + cCommcmds[pos], Commcmds[pos], "");
			mslRemote.update_tEvent("" + cCommcmds[pos], Commcmds[pos], null);
		} 
		else if (team=='A' && pos<10){
			send_to_basestation(cCTeamcmds[pos]);//<8
			scoreClients.update_tEvent("" + cCTeamcmds[pos], Teamcmds[pos], teamA.longName);
			mslRemote.update_tEvent("" + cCTeamcmds[pos], Teamcmds[pos], teamA);
		}
		else if (team=='B' && pos<10)
		{
			send_to_basestation(cMTeamcmds[pos]);//<8
			scoreClients.update_tEvent("" + cMTeamcmds[pos], Teamcmds[pos], teamB.longName);
			mslRemote.update_tEvent("" + cMTeamcmds[pos], Teamcmds[pos], teamB);
		}
	}
}

public static void test_send_direct(char team, int pos) {
	if (team=='C') BaseStationServer.write(cCommcmds[pos]);
	if (team=='A') BaseStationServer.write(cCTeamcmds[pos]);
	if (team=='B') BaseStationServer.write(cMTeamcmds[pos]);
}

public static boolean setteamfromip(String s) {
	String clientipstr="127.0.0.*";
	String[] iptokens;

	if (!s.equals("0:0:0:0:0:0:0:1")) {
		iptokens=split(s,'.');
		if (iptokens!=null) clientipstr=iptokens[0]+"."+iptokens[1]+"."+iptokens[2]+".*";
	}

	//println("Client IP: " + clientipstr);

	for (TableRow row : teamstable.rows()) {
		String saddr = row.getString("UnicastAddr");
		if (saddr.equals(clientipstr)) {
			println("Team " + row.getString("Team") + " connected (" + row.getString("shortname8") + "/" + row.getString("longame24") + ")");
			teamselect=row;
			
			boolean noTeamA = teamA.connectedClient == null || !teamA.connectedClient.active();
			boolean noTeamB = teamB.connectedClient == null || !teamB.connectedClient.active();
			
			if(StateMachine.GetCurrentGameState() == GameStateEnum.GS_PREGAME || (noTeamA || noTeamB)) // In pre-game or if lost all connections, ask for the color
			{
				Popup.show(PopupTypeEnum.POPUP_TEAMSELECTION, "Team: "+row.getString("Team")+"\nSelect color or press ESC to cancel",3, 0, 4, 16, 380, 200);
				return true;	
			}
			else
			{
				Log.logMessage("ERR No more connections allowed (Attempt from " + s + ")");
				return false;
			}
		}
	}
	Log.logMessage("ERR Unknown team (Attempt from " + s + ")");
	return false;
}

public static void checkBasestationsMessages()
{
	try
	{
		// Get the next available client
		Client thisClient = BaseStationServer.available();
		// If the client is not null, and says something, display what it said
		if (thisClient !=null) {
			
			Team t = null;
			int team = -1; // 0=A, 1=B
			if(teamA != null && teamA.connectedClient == thisClient)
			t=teamA;
			else if(teamB != null && teamB.connectedClient == thisClient)
			t=teamB;
			else{
				if(thisClient != connectingClient)
				println("NON TEAM MESSAGE RECEIVED FROM " + thisClient.ip());
				return;
			}
			String whatClientSaid = new String(thisClient.readBytes());
			if (whatClientSaid != null) 
			while(whatClientSaid.length() !=0){
				//println(whatClientSaid);
				int idx = whatClientSaid.indexOf('\0');
				//println(whatClientSaid.length()+"\t"+ idx);
				if(idx!=-1){
					if(idx!=0)
					{  
						t.wsBuffer+= whatClientSaid.substring(0,idx);
						if(idx < whatClientSaid.length())
						whatClientSaid = whatClientSaid.substring(idx+1);
						else
						whatClientSaid = "";
					}else{
						if(whatClientSaid.length() == 1)
						whatClientSaid = "";
						else
						whatClientSaid = whatClientSaid.substring(1);
					}
					
					// JSON Validation
					boolean ok = true;
					int ageMs = 0;
					String dummyFieldString;
					org.json.JSONArray dummyFieldJsonArray;
					try // Check for malformed JSON
					{
						t.worldstate_json = new org.json.JSONObject(t.wsBuffer);
					} catch(JSONException e) {
						String errorMsg = "ERROR malformed JSON (team=" + t.shortName + ") : " + t.wsBuffer;
						println(errorMsg);
						ok = false;
					}
					
					if(ok)
					{
						try // Check for "type" key
						{
							String type = t.worldstate_json.getString("type");
							
							// type must be "worldstate"
							if(!type.equals("worldstate"))
							{
								String errorMsg = "ERROR key \"type\" is not \"worldstate\" (team=" + t.shortName + ") : " + t.wsBuffer;
								println(errorMsg);
								ok = false;
							}
						} catch(JSONException e) {
							String errorMsg = "ERROR missing key \"type\" (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						try // Check for "ageMs" key
						{
							ageMs = t.worldstate_json.getInt("ageMs");
						} catch(JSONException e) {
							String errorMsg = "WS-ERROR missing key \"ageMs\" (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						try // Check for "teamName" key
						{
							dummyFieldString = t.worldstate_json.getString("teamName");
						} catch(JSONException e) {
							String errorMsg = "WS-ERROR missing key \"teamName\" (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						try // Check for "intention" key
						{
							dummyFieldString = t.worldstate_json.getString("intention");
						} catch(JSONException e) {
							String errorMsg = "WS-ERROR missing key \"intention\" (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						try // Check for "robots" key
						{
							dummyFieldJsonArray = t.worldstate_json.getJSONArray("robots");
						} catch(JSONException e) {
							String errorMsg = "WS-ERROR key \"robots\" is missing or is not array (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						try // Check for "balls" key
						{
							dummyFieldJsonArray = t.worldstate_json.getJSONArray("balls");
						} catch(JSONException e) {
							String errorMsg = "WS-ERROR key \"balls\" is missing or is not array (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						try // Check for "obstacles" key
						{
							dummyFieldJsonArray = t.worldstate_json.getJSONArray("obstacles");
						} catch(JSONException e) {
							String errorMsg = "WS-ERROR key \"obstacles\" is missing or is not array (team=" + t.shortName + ") : " + t.wsBuffer;
							println(errorMsg);
							ok = false;
						}
					}
					
					if(ok)
					{
						t.logWorldstate(t.wsBuffer,ageMs);
					}
					t.wsBuffer="";      
					//println("NEW message");
				}else{
					t.wsBuffer+= whatClientSaid;
					break;
				}
				//println("MESSAGE from " + thisClient.ip() + ": " + whatClientSaid);
				
				// Avoid filling RAM with buffering (for example team is not sending the '\0' character)
				if(t.wsBuffer.length() > 100000) {
					t.wsBuffer = "";
					String errorMsg = "ERROR JSON not terminated with '\\0' (team=" + t.shortName + ")";
					println(errorMsg);
				}
			}
			
			
		}
	}catch(Exception e){
	}
}

// -------------------------
// Referee Box Protocol 2015

// default commands
public static final char COMM_STOP = 'S';
public static final char COMM_START = 's';
public static final char COMM_WELCOME = 'W';  //NEW 2015CAMBADA: welcome message
public static final char COMM_RESET = 'Z';  //NEW 2015CAMBADA: Reset Game
public static final char COMM_TESTMODE_ON = 'U';  //NEW 2015CAMBADA: TestMode On
public static final char COMM_TESTMODE_OFF = 'u';  //NEW 2015CAMBADA: TestMode Off

// penalty Commands 
public static final char COMM_YELLOW_CARD_MAGENTA = 'y';  //NEW 2015CAMBADA: @remote
public static final char COMM_YELLOW_CARD_CYAN = 'Y';//NEW 2015CAMBADA: @remote
public static final char COMM_RED_CARD_MAGENTA = 'r';//NEW 2015CAMBADA: @remote
public static final char COMM_RED_CARD_CYAN = 'R';//NEW 2015CAMBADA: @remote
public static final char COMM_DOUBLE_YELLOW_MAGENTA = 'b'; //NEW 2015CAMBADA: exits field
public static final char COMM_DOUBLE_YELLOW_CYAN = 'B'; //NEW 2015CAMBADA:
//public static final char COMM_DOUBLE_YELLOW_IN_MAGENTA = 'j'; //NEW 2015CAMBADA: 
//public static final char COMM_DOUBLE_YELLOW_IN_CYAN = 'J'; //NEW 2015CAMBADA: 

// game flow commands
public static final char COMM_FIRST_HALF = '1';
public static final char COMM_SECOND_HALF = '2';
public static final char COMM_FIRST_HALF_OVERTIME = '3';  //NEW 2015CAMBADA: 
public static final char COMM_SECOND_HALF_OVERTIME = '4';  //NEW 2015CAMBADA: 
public static final char COMM_HALF_TIME = 'h';
public static final char COMM_END_GAME = 'e';    //ends 2nd part, may go into overtime
public static final char COMM_GAMEOVER = 'z';  //NEW 2015CAMBADA: Game Over
public static final char COMM_PARKING = 'L';

// goal status
public static final char COMM_GOAL_MAGENTA = 'a';
public static final char COMM_GOAL_CYAN = 'A';
public static final char COMM_SUBGOAL_MAGENTA = 'd';
public static final char COMM_SUBGOAL_CYAN = 'D';

// game flow commands
public static final char COMM_KICKOFF_MAGENTA = 'k';
public static final char COMM_KICKOFF_CYAN = 'K';
public static final char COMM_FREEKICK_MAGENTA = 'f';
public static final char COMM_FREEKICK_CYAN = 'F';
public static final char COMM_GOALKICK_MAGENTA = 'g';
public static final char COMM_GOALKICK_CYAN = 'G';
public static final char COMM_THROWIN_MAGENTA = 't';
public static final char COMM_THROWIN_CYAN = 'T';
public static final char COMM_CORNER_MAGENTA = 'c';
public static final char COMM_CORNER_CYAN = 'C';
public static final char COMM_PENALTY_MAGENTA = 'p';
public static final char COMM_PENALTY_CYAN = 'P';
public static final char COMM_DROPPED_BALL = 'N';

// repair Commands
public static final char COMM_REPAIR_OUT_MAGENTA = 'o';  //exits field
public static final char COMM_REPAIR_OUT_CYAN = 'O';

//free: 056789 iIfFHlmMnqQwxX
//------------------------------------------------------

public static StringDict Description;
public void comms_initDescriptionDictionary() {
	Description = new StringDict();
	Description.set("S", "STOP");
	Description.set("s", "START");
	Description.set("N", "Drop Ball");
	Description.set("h", "Halftime");
	Description.set("e", "End Game");
	Description.set("z", "Game Over");
	Description.set("Z", "Reset Game");
	Description.set("W", "Welcome");
	Description.set("U", "Test Mode on");
	Description.set("u", "Test Mode off");
	Description.set("1", "1st half");
	Description.set("2", "2nd half");
	Description.set("3", "Overtime 1st half");
	Description.set("4", "Overtime 2nd half");
	Description.set("L", "Park");

	Description.set("K", "CYAN Kickoff");
	Description.set("F", "CYAN Freekick");
	Description.set("G", "CYAN Goalkick");
	Description.set("T", "CYAN Throw In");
	Description.set("C", "CYAN Corner");
	Description.set("P", "CYAN Penalty Kick");
	Description.set("A", "CYAN Goal+");
	Description.set("D", "CYAN Goal-");
	Description.set("O", "CYAN Repair Out");
	Description.set("R", "CYAN Red Card");
	Description.set("Y", "CYAN Yellow Card");
	Description.set("B", "CYAN Double Yellow");

	Description.set("k", "MAGENTA Kickoff");
	Description.set("f", "MAGENTA Freekick");
	Description.set("g", "MAGENTA Goalkick");
	Description.set("t", "MAGENTA Throw In");
	Description.set("c", "MAGENTA Corner");
	Description.set("p", "MAGENTA Penalty Kick");
	Description.set("a", "MAGENTA Goal+");
	Description.set("d", "MAGENTA Goal-");
	Description.set("o", "MAGENTA Repair Out");
	Description.set("r", "MAGENTA Red Card");
	Description.set("y", "MAGENTA Yellow Card");
	Description.set("b", "MAGENTA Double Yellow");
}


static class Config
{
	// Networking
	public static int scoreClientsUpdatePeriod_ms = 1000;
	public static StringList scoreClientHosts = new StringList();
	public static IntList scoreClientPorts = new IntList();
	public static int remoteServerPort = 12345;
	public static int basestationServerPort = 28097;
	public static boolean remoteControlEnable = false;

	// Rules
	public static int repairPenalty_ms = 20000;                      //@mbc default value reajusted according to rules
	public static int doubleYellowPenalty_ms = 90000;                //@mbc default value reajusted according to rules
	public static int setPieceMaxTime_ms = 7000;

	// Appearance
	public static int maxShortName = 8;
	public static int maxLongName = 24;
	public static int robotPlayColor = 0xffE8FFD8;  //white (very light-green)
	public static int robotRepairColor = 0xff24287B;  //blue
	public static int robotYellowCardColor = 0xffFEFF0F;  //yellow  
	public static int robotDoubleYellowCardColor = 0xff707000;  //doubleyellow
	public static int robotRedCardColor = 0xffE03030;  //red
	public static String defaultCyanTeamShortName = "Team";
	public static String defaultCyanTeamLongName = "Cyan";
	public static int defaultCyanTeamColor = 0xff00ffff;
	public static String defaultMagentaTeamShortName = "Team";
	public static String defaultMagentaTeamLongName = "Magenta";
	public static int defaultMagentaTeamColor  = 0xffff00ff;

	// Sounds
	public static String sounds_maxTime = "";

	public static void Load(PApplet parent, String filename)
	{
		// file should be inside the "data" folder
		filename = parent.dataPath(filename);
		
		// Read json_string from file
		String json_string = null;
		try{
			BufferedReader reader = new BufferedReader(new FileReader(filename));
			String         line = null;
			StringBuilder  stringBuilder = new StringBuilder();
			String         ls = System.getProperty("line.separator");
			
			try {
				while( ( line = reader.readLine() ) != null ) {
					stringBuilder.append( line );
					stringBuilder.append( ls );
				}

				json_string = stringBuilder.toString();
			} finally {
				reader.close();
			}
		}catch(IOException e) {
			println("ERROR accessing file: " + e.getMessage());
			json_string = null;
		}
		
		// If json_string could be read correctly
		if(json_string != null)
		{
			org.json.JSONObject json_root = null;
			try // Check for malformed JSON
			{
				json_root = new org.json.JSONObject(json_string);
			} catch(JSONException e) {
				String errorMsg = "ERROR reading config file : malformed JSON";
				println(errorMsg);
				json_root = null;
			}
			
			// If JSON was correctly parsed
			if(json_root != null)
			{
				try // Get settings
				{
					org.json.JSONObject networking = json_root.getJSONObject("networking");
					org.json.JSONObject rules = json_root.getJSONObject("rules");
					org.json.JSONObject appearance = json_root.getJSONObject("appearance");
					org.json.JSONObject sounds = json_root.getJSONObject("sounds");
					
					// ----
					// Networking
					
					if(networking.has("scoreClientsUpdatePeriod_ms"))
					scoreClientsUpdatePeriod_ms = networking.getInt("scoreClientsUpdatePeriod_ms");
					
					if(networking.has("scoreClientsList"))
					{
						org.json.JSONArray listOfClients = networking.getJSONArray("scoreClientsList");
						for(int i = 0; i < listOfClients.length(); i++)
						{
							org.json.JSONObject client = listOfClients.getJSONObject(i);
							if(client.has("ip") && client.has("port"))
							{
								scoreClientHosts.append(client.getString("ip"));
								scoreClientPorts.append(client.getInt("port"));
							}
						}
					}
					
					if(networking.has("remoteServerPort"))
					remoteServerPort = networking.getInt("remoteServerPort");
					
					if(networking.has("basestationServerPort"))
					basestationServerPort = networking.getInt("basestationServerPort");
					

					
					if(networking.has("remoteControlEnable"))
					remoteControlEnable = networking.getBoolean("remoteControlEnable");
					
					// ----
					// Rules
					if(rules.has("repairPenalty_ms"))
					repairPenalty_ms = rules.getInt("repairPenalty_ms");
					
					if(rules.has("doubleYellowPenalty_ms"))
					doubleYellowPenalty_ms = rules.getInt("doubleYellowPenalty_ms");
					
					if(rules.has("setPieceMaxTime_ms"))
					setPieceMaxTime_ms = rules.getInt("setPieceMaxTime_ms");
					
					// ----
					// Appearance
					if(appearance.has("maxShortName"))
					maxShortName = appearance.getInt("maxShortName");
					
					if(appearance.has("maxLongName"))
					maxLongName = appearance.getInt("maxLongName");
					
					if(appearance.has("robotPlayColor"))
					robotPlayColor = string2color(appearance.getString("robotPlayColor"));
					
					if(appearance.has("robotRepairColor"))
					robotRepairColor = string2color(appearance.getString("robotRepairColor"));
					
					if(appearance.has("robotYellowCardColor"))
					robotYellowCardColor = string2color(appearance.getString("robotYellowCardColor"));
					
					if(appearance.has("robotDoubleYellowCardColor"))
					robotDoubleYellowCardColor = string2color(appearance.getString("robotDoubleYellowCardColor"));
					
					if(appearance.has("robotRedCardColor"))
					robotRedCardColor = string2color(appearance.getString("robotRedCardColor"));
					
					if(appearance.has("defaultCyanTeamShortName"))
					defaultCyanTeamShortName = appearance.getString("defaultCyanTeamShortName");

					if(appearance.has("defaultCyanTeamLongName"))
					defaultCyanTeamLongName = appearance.getString("defaultCyanTeamLongName");
					
					if(appearance.has("defaultCyanTeamColor"))
					defaultCyanTeamColor = string2color(appearance.getString("defaultCyanTeamColor"));

					
					if(appearance.has("defaultMagentaTeamShortName"))
					defaultMagentaTeamShortName = appearance.getString("defaultMagentaTeamShortName");

					if(appearance.has("defaultMagentaTeamLongName"))
					defaultMagentaTeamLongName = appearance.getString("defaultMagentaTeamLongName");
					
					if(appearance.has("defaultMagentaTeamColor"))
					defaultMagentaTeamColor = string2color(appearance.getString("defaultMagentaTeamColor"));
					
					// ----
					// Sounds
					if(sounds.has("maxSetPieceTime"))
					sounds_maxTime = sounds.getString("maxSetPieceTime");
					
				} catch(JSONException e) {
					String errorMsg = "ERROR reading config file...";
					println(errorMsg);
				}
				
			}
		}
		
		if (scoreClientsUpdatePeriod_ms<50) scoreClientsUpdatePeriod_ms=50;
		
		printConfig();
	}

	public static void printConfig()
	{
		// Networking
		println( "### Networking ###" );
		println( "scoreClientsUpdatePeriod_ms  : " + scoreClientsUpdatePeriod_ms);
		println( "scoreClients                 : " + scoreClientHosts.size());
		for(int i = 0; i < scoreClientHosts.size(); i++)
		println( "    " + scoreClientHosts.get(i) + ":" + scoreClientPorts.get(i));
		
		println( "remoteServerPort             : " + remoteServerPort);
		println( "basestationServerPort        : " + basestationServerPort);
		println( "remoteControlEnable          : " + remoteControlEnable); 
		println();
		// Rules
		println( "### Rules ###" );
		println( "repairPenalty_ms             : " + repairPenalty_ms);
		println( "doubleYellowPenalty_ms       : " + doubleYellowPenalty_ms);
		println();
		// Appearance
		println( "### Appearance ###" );
		println( "maxShortName                 : " + maxShortName);
		println( "maxLongName                  : " + maxLongName);
		println( "robotPlayColor               : " + color2string(robotPlayColor));
		println( "robotRepairColor             : " + color2string(robotRepairColor));
		println( "robotYellowCardColor         : " + color2string(robotYellowCardColor));  
		println( "robotDoubleYellowCardColor   : " + color2string(robotDoubleYellowCardColor));
		println( "robotRedCardColor            : " + color2string(robotRedCardColor));
		println( "defaultCyanTeamShortName     : " + defaultCyanTeamShortName);
		println( "defaultCyanTeamLongName      : " + defaultCyanTeamLongName);
		println( "defaultCyanTeamColor         : " + color2string( defaultCyanTeamColor));
		println( "defaultMagentaTeamShortName  : " + defaultMagentaTeamShortName );
		println( "defaultMagentaTeamLongName   : " + defaultMagentaTeamLongName );
		println( "defaultMagentaTeamColor      : " + color2string( defaultMagentaTeamColor ));
		// Sounds
		println( "### Sounds ###" );
		println( "sounds_maxTime                 : " + sounds_maxTime);
		
	}
}



static class Log
{
	public static boolean enable = true;
	private static PApplet parent = null;
	private static String currentTimedName = "";

	public static void init(PApplet p)
	{
		Log.parent = p;
		createLog();
	}

	private static String getTimedName()
	{
		return currentTimedName;
	}

	private static String createTimedName()
	{
		return nf(year(),4)+nf(month(),2)+nf(day(),2)+"_"+nf(hour(),2)+nf(minute(),2)+nf(second(),2);
	}

	public static void createLog() {
		currentTimedName = createTimedName();
		LogFileName=currentTimedName + ".msl";  
		screenlog("Logfile "+LogFileName);
		screenlog("Logging is "+(Log.enable ? "enabled":"disabled"));
		//println("LOG_FILENAME "+LogFileName);
	}

	public static void appendTextToFile(String filename, String text) {
		if(parent == null)
		return;
		
		File f = new File(parent.dataPath("tmp/" + filename));
		if (!f.exists()) {
			createFile(f);
		}
		try {
			PrintWriter out = new PrintWriter(new BufferedWriter(new FileWriter(f, true)));
			out.println(text);
			out.close();
		}
		catch (IOException e) {
			e.printStackTrace();
		}
	}

	// Log to screen only
	public static void screenlog(String s){
		for (int i=4; i>0; i--)
		Last5cmds[i]=Last5cmds[i-1];
		
		String newLog = nf(hour(),2)+":"+nf(minute(),2)+":"+nf(second(),2)+" "+s;
		if(newLog.length() > 41)
		newLog = newLog.substring(0,40);
		Last5cmds[0]=newLog;
	}

	// Log action to both screen and logfile
	public static void logactions(String c) {
		String s1=Description.get(c+"");
		String s2=System.currentTimeMillis()+","+gametime+"("+gameruntime+"),"+StateMachine.GetCurrentGameStateString()+","+c+","+Description.get(c+"");
		lastaction=c;

		screenlog(s1);
		if (Log.enable) Log.appendTextToFile(LogFileName,s2);
		
	}

	// Log message to both screen and logfile
	// This function is never used
	public static void logMessage(String s)
	{
		screenlog(s);  
		if (Log.enable) Log.appendTextToFile(LogFileName,s);
	}

	public static void createFile(File f) {
		File parentDir = f.getParentFile();
		try {
			parentDir.mkdirs(); 
			f.createNewFile();
		}
		catch(Exception e) {
			e.printStackTrace();
		}
	} 
}



/**
* Log Merger
*   Ricardo Dias <ricardodias@ua.pt>
*
* This class is responsible for merging worldstate information 
* that come from the teams during the match into a single file.
* 
* Based on the timedName, it will search files from both teams
* on the "data" directory.
*/

static class LogMerger
{

	// ---
	// Atributes
	private org.json.JSONArray tA = null;
	private org.json.JSONArray tB = null;
	private org.json.JSONArray merged = null;
	private String timedName;
	private String teamAName;
	private String teamBName;
	static final int BUFFER = 2048;
	// ---


	// ---
	// Constructor
	public LogMerger(String timedName)
	{
		this.timedName = timedName;
		
		File tAFile = new File(mainApplet.dataPath("tmp/" + timedName + ".A.msl"));
		File tBFile = new File(mainApplet.dataPath("tmp/" + timedName + ".B.msl"));
		
		tA = parseFile(tAFile);
		tB = parseFile(tBFile);
		merged = new org.json.JSONArray();
	}
	// ---


	// ---
	// Parses a File object into a JSONArray
	private org.json.JSONArray parseFile(File file)
	{
		org.json.JSONArray ret = null;
		try
		{
			BufferedReader br = new BufferedReader(new FileReader(file));
			ret = new org.json.JSONArray(new org.json.JSONTokener(br));
		} catch(Exception e) {
			println("ERROR: Problem with file " + file.getAbsolutePath());
		}
		return ret;
	}
	// ---


	// ---
	// Merges the two arrays into one
	public void merge()
	{
		merged = new org.json.JSONArray();
		try
		{
			if(tA == null && tB != null)          // problem with file from team A, merge = teamB
			merged = tB;
			else if(tB == null && tA != null)     // problem with file from team A, merge = teamB
			merged = tA;
			else if(tA != null && tB != null) {   // normal merge
				println("Merging log files...");  
				
				int sizeA = tA.length();
				int sizeB = tB.length();
				
				int iA = 0;
				int iB = 0;
				int nFrames = 0;
				while(nFrames < sizeA + sizeB)
				{
					org.json.JSONObject selected = null;
					if(iA == sizeA) {                   // no more samples from team A
						selected = tB.getJSONObject(iB);
						iB++;
						teamBName = selected.optString("teamName", teamBName);
					} else if(iB == sizeB) {            // no more samples from team B
						selected = tA.getJSONObject(iA);
						iA++;
						teamAName = selected.optString("teamName", teamAName);
					} else {
						org.json.JSONObject oA = tA.getJSONObject(iA);
						org.json.JSONObject oB = tB.getJSONObject(iB);
						if(oA.getInt("timestamp") < oB.getInt("timestamp"))
						{
							selected = tA.getJSONObject(iA);
							iA++;
							teamAName = selected.optString("teamName", teamAName);
						}else{
							selected = tB.getJSONObject(iB);
							iB++;
							teamBName = selected.optString("teamName", teamBName);
						}
					}
					if(selected != null)
					merged.put(selected);
					
					nFrames++;
					println("Merging log files... ["+ nFrames*100.0f/(sizeA+sizeB) +"%]");
				}
			}
		}catch(Exception e) {
			e.printStackTrace();
			return;
		}
		
		writeMergedFile();
		zipAllFiles();
	}
	// ---


	// ---
	// Write merged file into folder
	private boolean writeMergedFile()
	{
		try
		{
			println("Writing merge to file...");
			FileWriter writer = new FileWriter(new File(mainApplet.dataPath("tmp/" + timedName + ".merged.msl")));
			writer.write(merged.toString());
			writer.close();
			println("DONE!");
		} catch(Exception e) {
			println("ERROR Writing merged log file");
			e.printStackTrace();
			return false;
		}
		return true;
	}
	// ---


	// ---
	// Zip all
	public boolean zipAllFiles()
	{    
		try
		{
			println("Zipping game log files...");
			BufferedInputStream origin = null;
			FileOutputStream dest = new FileOutputStream(mainApplet.dataPath("logs/" + timedName + "." + teamAName + "-" + teamBName + ".zip"));
			ZipOutputStream out = new ZipOutputStream(new BufferedOutputStream(dest));
			out.setMethod(ZipOutputStream.DEFLATED);
			byte data[] = new byte[BUFFER];
			
			//String[] files = {".msl", ".A.msl", ".B.msl", ".merged.msl"};
			String[] files = {".msl", ".A.msl", ".B.msl"};
			for(int i = 0; i < files.length; i++)
			{
				String fileName = mainApplet.dataPath("tmp/" + timedName + files[i]);
				File f = new File(fileName);
				if(!f.exists() || !f.isFile())
				continue;
				
				println("Adding file " + files[i]);
				FileInputStream fi = new FileInputStream(fileName);
				origin = new BufferedInputStream(fi, BUFFER);
				ZipEntry entry = new ZipEntry(timedName + files[i]);
				out.putNextEntry(entry);
				int count;
				while((count = origin.read(data, 0, BUFFER)) != -1) {
					out.write(data, 0, count);
				}
				origin.close();
			}
			out.close();
			println("DONE! \"" + timedName + "." + teamAName + "-" + teamBName + ".zip\" created");
			
		} catch(Exception e) {
			println("ERROR Zipping log files");
			e.printStackTrace();
			return false;
		}
		return true;
	}
	// ---

}
class MSLRemote
{
	public MyServer server;
	private String lastCommand = " ";

	private static final String GAMESTATUS_PRE_GAME = "Va";
	private static final String GAMESTATUS_PRE_GAME_KICK_OFF_CYAN = "Vb";
	private static final String GAMESTATUS_PRE_GAME_KICK_OFF_MAGENTA = "Vc";
	private static final String GAMESTATUS_GAME_STOP_HALF1 = "Vd";
	private static final String GAMESTATUS_GAME_STOP_HALF2 = "Ve";
	private static final String GAMESTATUS_GAME_ON_HALF1 = "Vf";
	private static final String GAMESTATUS_GAME_ON_HALF2 = "Vg";
	private static final String GAMESTATUS_HALF_TIME = "Vh";
	private static final String GAMESTATUS_HALF_KICK_OFF_CYAN = "Vi";
	private static final String GAMESTATUS_HALF_KICK_OFF_MAGENTA = "Vj";
	private static final String GAMESTATUS_END_GAME = "Vk";
	private static final String GAMESTATUS_SET_PLAY = "Vl";

	// Set plays:
	private static final String COMMAND_KICK_OFF_CYAN = "CK";
	private static final String COMMAND_FREEKICK_CYAN = "CF";
	private static final String COMMAND_THROW_IN_CYAN = "CT";
	private static final String COMMAND_GOALKICK_CYAN = "CG";
	private static final String COMMAND_CORNER_CYAN = "CE";
	private static final String COMMAND_PENALTY_CYAN = "CP";
	private static final String COMMAND_SCORE_CYAN = "CL"; // append score 2 digits (e.g. "CL01" for cyan score = 1)
	private static final String COMMAND_YELLOW_CYAN = "CY1"; // robot 1
	private static final String COMMAND_OUT_CYAN = "Co1"; // robot 1

	private static final String COMMAND_KICK_OFF_MAGENTA= "MK";
	private static final String COMMAND_FREEKICK_MAGENTA = "MF";
	private static final String COMMAND_THROW_IN_MAGENTA = "MT";
	private static final String COMMAND_GOALKICK_MAGENTA = "MG";
	private static final String COMMAND_CORNER_MAGENTA = "ME";
	private static final String COMMAND_PENALTY_MAGENTA = "MP";
	private static final String COMMAND_SCORE_MAGENTA = "ML"; // append score 2 digits (e.g. "CL01" for cyan score = 1)
	private static final String COMMAND_YELLOW_MAGENTA = "MY1"; // robot 1
	private static final String COMMAND_OUT_MAGENTA = "Mo1"; // robot 1

	private static final String COMMAND_DROPBALL = "SD";
	private static final String COMMAND_START = "ST";
	private static final String COMMAND_STOP = "SP";
	private static final String COMMAND_ENDPART = "SG";
	private static final String COMMAND_RESET = "SR";



	public MSLRemote(PApplet parent, int port)
	{
		server = new MyServer(parent, port);
	}

	public void setLastCommand(String cmd)
	{
		lastCommand = cmd;
	}

	public String getEventCommand()
	{
		if(lastCommand.equals(cCommcmds[CMDID_COMMON_START]))
		return COMMAND_START;
		else if(lastCommand.equals(cCommcmds[CMDID_COMMON_STOP]))
		return COMMAND_STOP;
		else if(lastCommand.equals(cCommcmds[CMDID_COMMON_DROP_BALL]))
		return COMMAND_DROPBALL;
		else if(lastCommand.equals(cCommcmds[CMDID_COMMON_HALFTIME]))
		return COMMAND_ENDPART;
		else if(lastCommand.equals(cCommcmds[CMDID_COMMON_RESET]))
		return COMMAND_RESET;
		
		else if(lastCommand.equals(cCTeamcmds[CMDID_TEAM_KICKOFF]))
		return COMMAND_KICK_OFF_CYAN;
		else if(lastCommand.equals(cCTeamcmds[CMDID_TEAM_FREEKICK]))
		return COMMAND_FREEKICK_CYAN;
		else if(lastCommand.equals(cCTeamcmds[CMDID_TEAM_GOALKICK]))
		return COMMAND_GOALKICK_CYAN;
		else if(lastCommand.equals(cCTeamcmds[CMDID_TEAM_THROWIN]))
		return COMMAND_THROW_IN_CYAN;
		else if(lastCommand.equals(cCTeamcmds[CMDID_TEAM_CORNER]))
		return COMMAND_CORNER_CYAN;
		else if(lastCommand.equals(cCTeamcmds[CMDID_TEAM_PENALTY]))
		return COMMAND_PENALTY_CYAN;
		
		else if(lastCommand.equals(cMTeamcmds[CMDID_TEAM_KICKOFF]))
		return COMMAND_KICK_OFF_MAGENTA;
		else if(lastCommand.equals(cMTeamcmds[CMDID_TEAM_FREEKICK]))
		return COMMAND_FREEKICK_MAGENTA;
		else if(lastCommand.equals(cMTeamcmds[CMDID_TEAM_GOALKICK]))
		return COMMAND_GOALKICK_MAGENTA;
		else if(lastCommand.equals(cMTeamcmds[CMDID_TEAM_THROWIN]))
		return COMMAND_THROW_IN_MAGENTA;
		else if(lastCommand.equals(cMTeamcmds[CMDID_TEAM_CORNER]))
		return COMMAND_CORNER_MAGENTA;
		else if(lastCommand.equals(cMTeamcmds[CMDID_TEAM_PENALTY]))
		return COMMAND_PENALTY_MAGENTA;
		
		return "";
	}

	public String getGameStatusCommand()
	{    
		// 0  "Pre-Game",
		// 1  "Game - 1st Half",
		// 2  "Game - Halftime",
		// 3  "Game - 2nd Half", 
		// 4  "Game - End", 
		// 5  "Overtime - 1st",
		// 6  "Overtime - Switch",
		// 7  "Overtime - 2nd",
		// 8  "Penalty",
		// 9  "GameOver"
		
		GameStateEnum gs = StateMachine.GetCurrentGameState(); 
		
		boolean kickoff = false;
		boolean teamCyan = false;
		
		if(lastCommand == cCTeamcmds[CMDID_TEAM_KICKOFF] || lastCommand == cMTeamcmds[CMDID_TEAM_KICKOFF])
		{
			if(gs == GameStateEnum.GS_PREGAME || gs == GameStateEnum.GS_HALFTIME || gs == GameStateEnum.GS_HALFTIME_OVERTIME) // pre or halftime
			{
				kickoff = true;
				if(lastCommand == cCTeamcmds[CMDID_TEAM_KICKOFF])
				teamCyan = true;
			}
		}else if(lastCommand == cCTeamcmds[CMDID_TEAM_FREEKICK]
				|| lastCommand == cCTeamcmds[CMDID_TEAM_GOALKICK]
				|| lastCommand == cCTeamcmds[CMDID_TEAM_THROWIN]
				|| lastCommand == cCTeamcmds[CMDID_TEAM_CORNER]
				|| lastCommand == cCTeamcmds[CMDID_TEAM_PENALTY]
				|| lastCommand == cMTeamcmds[CMDID_TEAM_FREEKICK]
				|| lastCommand == cMTeamcmds[CMDID_TEAM_GOALKICK]
				|| lastCommand == cMTeamcmds[CMDID_TEAM_THROWIN]
				|| lastCommand == cMTeamcmds[CMDID_TEAM_CORNER]
				|| lastCommand == cMTeamcmds[CMDID_TEAM_PENALTY]
				|| lastCommand == cCommcmds[CMDID_COMMON_DROP_BALL])
		return GAMESTATUS_SET_PLAY;

		switch(gs)
		{
		case GS_PREGAME:
			if(kickoff)
			{
				if(teamCyan)
				return GAMESTATUS_PRE_GAME_KICK_OFF_CYAN;
				else
				return GAMESTATUS_PRE_GAME_KICK_OFF_MAGENTA;
			}
			return GAMESTATUS_PRE_GAME;
			
			
		case GS_GAMESTOP_H1:
		case GS_GAMESTOP_H3:
			return GAMESTATUS_GAME_STOP_HALF1;
			
		case GS_GAMEON_H1:
		case GS_GAMEON_H3:
			return GAMESTATUS_GAME_ON_HALF1;

		case GS_HALFTIME:
		case GS_OVERTIME:
		case GS_HALFTIME_OVERTIME:
			if(kickoff)
			{
				if(teamCyan)
				return GAMESTATUS_HALF_KICK_OFF_CYAN;
				else
				return GAMESTATUS_HALF_KICK_OFF_MAGENTA;
			}
			return GAMESTATUS_HALF_TIME;
			
		case GS_GAMESTOP_H2:
		case GS_GAMESTOP_H4:
			return GAMESTATUS_GAME_STOP_HALF2;
			
		case GS_GAMEON_H2:
		case GS_GAMEON_H4:
			return GAMESTATUS_GAME_ON_HALF2;

		case GS_PENALTIES:
			return GAMESTATUS_GAME_STOP_HALF2;
			
		case GS_ENDGAME:
			return GAMESTATUS_END_GAME;
		}
		
		return "";
	}

	// Sends an "event" type update message to the clients
	public void update_tEvent(String eventCode, String eventDesc, Team team)
	{
		int teamId = -1;
		if(team == teamA) teamId = 0;
		else if(team == teamB) teamId = 1;
		
		int scoreA = (teamA != null) ? teamA.Score : 0;
		int scoreB = (teamB != null) ? teamB.Score : 0;
		
		String msg = "{";
		msg += "\"type\": \"event\",";
		msg += "\"eventCode\": \"" + eventCode + "\",";
		msg += "\"eventDesc\": \"" + eventDesc + "\",";
		msg += "\"teamId\": " + teamId + ",";
		msg += "\"teamName\": \"" + ((teamId == -1) ? "" : team.shortName) + "\",";
		msg += "\"gamestatus\": \"" + getGameStatusCommand() + "\",";
		msg += "\"command\": \"" + getEventCommand() + "\",";
		msg += "\"scoreTeamA\": " + scoreA + ",";
		msg += "\"scoreTeamB\": " + scoreB;
		msg += "}";
		msg += (char)0x00;
		
		writeMsg(msg);
	}



	public int clientCount()
	{
		return server.clientCount;
	}

	public void stopServer()
	{
		server.stop();
	}

	public void writeMsg(String message)
	{
		if (server.clientCount > 0){
			server.write(message);
		}
	}

	public void checkMessages()
	{
		try
		{
			// Get the next available client
			Client thisClient = server.available();
			// If the client is not null, and says something, display what it said
			if (thisClient !=null) {
				String whatClientSaid = thisClient.readString();
				if (whatClientSaid != null) {
					
					println("MSL Remote JSON: " + whatClientSaid);
					
					org.json.JSONObject jsonObj = new org.json.JSONObject(whatClientSaid);
					
					int pos = jsonObj.getInt("id");
					
					char group = 'C';
					
					buttonEvent(group, pos);
					
				}
			}
			
		}catch(Exception e){
			println("Invalid JSON received from MSL Remote.");
		}
	}
}

public static void resumeSplitTimer() {				 // Used in StateMachine
	if(stopsplittimer)
	{
		tsplitTime = System.currentTimeMillis();
		stopsplittimer=false;
	}
}

// --------------------------------------------------

public void setbackground() {
	rectMode(CENTER);
	textAlign(CENTER, CENTER);

	background(48);

	//center rect
	fill(0, 32);
	stroke(255, 32);
	//rect(400, 288, 256, 208, 16);
	fill(0,16);
	rect(width/2, height/2+28, 256, 300, 16);

	// dividers
	int ramp=34;
	float offsetx=0.35f*width-ramp;
	float offsety=112;
	float m=0.3f;

	//top cyan
	strokeWeight(2);
	fill(Config.defaultCyanTeamColor); 
	stroke(0);
	beginShape();
	vertex(0, 0);
	vertex(0, offsety);
	vertex(offsetx, offsety);
	vertex(offsetx, 0);
	endShape();

	//top magenta
	strokeWeight(2);
	fill(Config.defaultMagentaTeamColor); 
	stroke(0);
	beginShape();
	vertex(width, 0);
	vertex(width, offsety);
	vertex(width-offsetx, offsety);
	vertex(width-offsetx, 0);
	endShape();


	//top fill
	fill(96);
	beginShape();
	vertex(offsetx+2, 0);
	vertex(offsetx+2, offsety);
	vertex(offsetx+m*ramp+2, offsety+ramp-1);
	vertex(width-1-offsetx-m*ramp-1, offsety+ramp-1);
	vertex(width-1-offsetx-1, offsety);
	vertex(width-1-offsetx-1, 0);
	endShape();


	//bottom
	strokeWeight(2);
	fill(96);
	stroke(0);
	offsety=height-1-128+48;
	beginShape();
	vertex(1, height-2);
	vertex(1, offsety);
	vertex(offsetx, offsety);
	vertex(offsetx+m*ramp, offsety-ramp);
	vertex(width-1-offsetx-m*ramp, offsety-ramp);
	vertex(width-1-offsetx, offsety);
	vertex(width-2, offsety);
	vertex(width-2, height-2);
	vertex(1, height-2);
	endShape();

	//bottom fill
	fill(96);
	beginShape();
	vertex(offsetx+2,height);
	vertex(offsetx+2,offsety+1);
	vertex(offsetx+m*ramp+2,offsety-ramp+2);
	vertex(width-1-offsetx-m*ramp-1,offsety-ramp+2);
	vertex(width-1-offsetx-1,offsety+1);
	vertex(width-1-offsetx-1,height);
	endShape();

	//carbon
	stroke(0,128);
	for(int i=0; i<width*2; i+=4)
	line(0,i,i,0);

	backgroundImage=get();
}

public static int string2color(String hex_string)
{
	int col = 0;
	if (trim(hex_string).charAt(0)=='#')	col=unhex("FF"+trim(hex_string).substring(1));
	return col;
}

public static String color2string(int col)
{
	String ret;
	ret = "" + hex(col);
	ret = "#" + ret.substring(2);
	return ret;
}
static class Popup
{
	private static boolean enabled = false;
	private static PopupTypeEnum type;
	private static boolean newResponse = false;
	private static String lastResponse = "";
	private static int numOfButtons;

	private static String message = "";
	private static String btnLeft = "";
	private static String btnCenter = "";
	private static String btnRight = "";

	private static int b1;
	private static int b2;
	private static int b3;
	private static int bw1;
	private static int bw2;
	private static int bw3;

	private static int fontSize;

	private static int popUpWidth = 380;
	private static int popUpHeight = 200;

	// Methods
	public static boolean isEnabled() { return enabled; }

	public static boolean hasNewResponse() {
		boolean resp = newResponse;
		newResponse = false;  
		return resp;
	}
	public static String getResponse() { return lastResponse; }

	public static PopupTypeEnum getType() { return type; }

	public static void show(PopupTypeEnum type, String message, int bt1, int bt2, int bt3, int fs, int ww, int hh) {
		Popup.type = type;
		Popup.message = message;
		Popup.btnLeft = btnLeft;
		Popup.btnCenter = btnCenter;
		Popup.btnRight = btnRight;
		numOfButtons = 0;
		fontSize = fs;
		popUpWidth = ww;
		popUpHeight = hh;
		
		b1 = bt1; bw1 = 0;
		b2 = bt2; bw2 = 0;
		b3 = bt3; bw3 = 0;
		
		if (bt1 > 0) {bPopup[bt1].enable(); bw1 = bPopup[bt1].bwidth; numOfButtons++;}
		if (bt2 > 0) {bPopup[bt2].enable(); bw2 = bPopup[bt2].bwidth; numOfButtons++;} 
		if (bt3 > 0) {bPopup[bt3].enable(); bw3 = bPopup[bt3].bwidth; numOfButtons++;}
		enabled = true;
		mainApplet.redraw();
	}

	public static void close()
	{
		for (int n = 0; n < popUpButtons; n++)
		bPopup[n].disable();
		enabled = false;
		mainApplet.redraw();
		
		// If connectingClient is still referencing a client when closing popup, we have to close the connection
		if(connectingClient != null){
			connectingClient.stop();
			connectingClient = null;
		}
	}

	public static void check(boolean mousePress) {
		// check mouse over
		if (bPopup[b1].isEnabled()) bPopup[b1].checkhover();
		if (bPopup[b2].isEnabled()) bPopup[b2].checkhover();
		if (bPopup[b3].isEnabled()) bPopup[b3].checkhover();
		
		if(mousePress)
		{
			if (bPopup[b1].HOVER == true) bPopup[b1].activate();
			if (bPopup[b2].HOVER == true) bPopup[b2].activate();
			if (bPopup[b3].HOVER == true) bPopup[b3].activate();
			if (bPopup[b1].isActive()) {
				lastResponse = bPopup[b1].Label;
				newResponse = true;
			}
			if (bPopup[b2].isActive()) {
				lastResponse = bPopup[b2].Label;
				newResponse = true;
			}
			if (bPopup[b3].isActive()) {
				lastResponse = bPopup[b3].Label;
				newResponse = true;
			}
		}
	}

	public static void draw() {
		
		mainApplet.rectMode(CENTER);

		mainApplet.noStroke();
		mainApplet.fill(255, 80); //,224
		mainApplet.rect(mainApplet.width/2 + 6, mainApplet.height/2 + 6,  popUpWidth, popUpHeight, 12);		

		mainApplet.strokeWeight(2);
		mainApplet.fill(63, 72, 204); mainApplet.stroke(220, 220, 220);
		mainApplet.rect(mainApplet.width/2, mainApplet.height/2, popUpWidth, popUpHeight, 12);		
		
		int hw = 0;
		if (bw1 > 0) hw = bw1 / 2;
		else if (bw2 > 0) hw = bw2 / 2;
		else hw = bw3 / 2;
		
		int delta = (popUpWidth - bw1 - bw2 - bw3) / (numOfButtons + 1);
		int leftOffset = (mainApplet.width / 2 - popUpWidth / 2) + delta + hw;
		if (bPopup[b1].isEnabled()) {
			if (type == PopupTypeEnum.POPUP_HELP) {
				bPopup[b1].setxy(leftOffset, mainApplet.height/2+78);
			}
			else {
				bPopup[b1].setxy(leftOffset, mainApplet.height/2+40);
			}
			leftOffset += (delta + bw1);  
		}
		if (bPopup[b2].isEnabled()) {
			bPopup[b2].setxy(leftOffset, mainApplet.height/2+40);
			leftOffset += (delta + bw2);  
		}
		if (bPopup[b3].isEnabled()) {
			bPopup[b3].setxy(leftOffset, mainApplet.height/2+40);
		}
		
		mainApplet.fill(220);
		mainApplet.textFont(panelFont);
		mainApplet.textAlign(CENTER, CENTER);
		mainApplet.textSize(fontSize);
		
		if (type == PopupTypeEnum.POPUP_HELP) {
			mainApplet.textAlign(LEFT, CENTER);
			mainApplet.text( message, mainApplet.width/2 - 205 , mainApplet.height/2 - 35);
		}
		else if (type == PopupTypeEnum.POPUP_WAIT){
			mainApplet.text( message, mainApplet.width/2, mainApplet.height/2);
		}
		else {
			mainApplet.text( message, mainApplet.width/2, mainApplet.height/2 - 50);
		}
		
		if (bPopup[b1].isEnabled()) bPopup[b1].checkhover();
		if (bPopup[b2].isEnabled()) bPopup[b2].checkhover();
		if (bPopup[b3].isEnabled()) bPopup[b3].checkhover();

		if (bPopup[b1].isEnabled()) bPopup[b1].update();
		if (bPopup[b2].isEnabled()) bPopup[b2].update();
		if (bPopup[b3].isEnabled()) bPopup[b3].update();
	}

}
//*********************************************************************
public void RefreshButonStatus1() {

	switch(StateMachine.GetCurrentGameState())
	{
		// PRE-GAME
	case GS_PREGAME:
	//	if (Popup.isEnabled() && (Popup.getType().getValue() == 6)) Popup.close();
		if (Popup.isEnabled() && (Popup.getType() == PopupTypeEnum.POPUP_WAIT)) Popup.close();
		
		buttonAdisableAll(0);  //team A commands	
		buttonBdisableAll(0);  //team B commands
		buttonCdisable();     //common commands
		
		if(StateMachine.setpiece)
		{
			if(StateMachine.setpiece_cyan){
				buttonFromEnum(ButtonsEnum.BTN_C_KICKOFF).activate();
				buttonFromEnum(ButtonsEnum.BTN_M_KICKOFF).disable();

			}else{
				buttonFromEnum(ButtonsEnum.BTN_C_KICKOFF).disable();
				buttonFromEnum(ButtonsEnum.BTN_M_KICKOFF).activate();
			}
			
			buttonFromEnum(ButtonsEnum.BTN_RESET).disable();
			buttonFromEnum(ButtonsEnum.BTN_START).enable();
			buttonFromEnum(ButtonsEnum.BTN_STOP).activate();
		}else{
			buttonFromEnum(ButtonsEnum.BTN_C_KICKOFF).enable();
			buttonFromEnum(ButtonsEnum.BTN_M_KICKOFF).enable();

			buttonFromEnum(ButtonsEnum.BTN_START).disable();
			buttonFromEnum(ButtonsEnum.BTN_STOP).activate();
			buttonFromEnum(ButtonsEnum.BTN_RESET).activate();
		}
		

		break;
		
	case GS_GAMEON_H1:
	case GS_GAMEON_H2:
	case GS_GAMEON_H3:
	case GS_GAMEON_H4:
		refreshbutton_game_on();
		break;
		
	case GS_GAMESTOP_H1:
	case GS_GAMESTOP_H2:
	case GS_GAMESTOP_H3:
	case GS_GAMESTOP_H4:
		refreshbutton_game_stopped();
		if(StateMachine.setpiece){
			buttonAdisable();  //team A commands
			buttonBdisable();  //team B commands
			buttonCdisable();  //common commands
			buttonFromEnum(StateMachine.setpiece_button).activate();
			buttonFromEnum(ButtonsEnum.BTN_START).enable();
			buttonFromEnum(ButtonsEnum.BTN_PARK).disable();
		}else{
			buttonFromEnum(ButtonsEnum.BTN_START).disable();
		}
		break;
		
	case GS_HALFTIME:
	case GS_OVERTIME:
	case GS_HALFTIME_OVERTIME:
		buttonAdisableAll(0);  //team A commands
		buttonBdisableAll(0);  //team B commands
		buttonCdisable();     //common commands
		bTeamAcmds[CMDID_TEAM_GOAL].disable();		// Disable goal button if part ended with a goal
		bTeamBcmds[CMDID_TEAM_GOAL].disable();		// Disable goal button if part ended with a goal

		// Alternate Kick-Offs
		boolean enableCyan = StateMachine.firstKickoffCyan;
		if(StateMachine.GetCurrentGameState() == GameStateEnum.GS_HALFTIME || StateMachine.GetCurrentGameState() == GameStateEnum.GS_HALFTIME_OVERTIME)
		enableCyan = !enableCyan;

		if(StateMachine.setpiece)
		{
			buttonFromEnum(StateMachine.setpiece_button).activate();
			buttonFromEnum(ButtonsEnum.BTN_START).enable();
			buttonFromEnum(ButtonsEnum.BTN_STOP).activate();
			buttonFromEnum(ButtonsEnum.BTN_PARK).disable();
			buttonFromEnum(ButtonsEnum.BTN_RESET).disable();
		}else{
			if(enableCyan)
			{
				buttonFromEnum(ButtonsEnum.BTN_C_KICKOFF).enable();
				buttonFromEnum(ButtonsEnum.BTN_M_KICKOFF).disable();
			}else{
				buttonFromEnum(ButtonsEnum.BTN_C_KICKOFF).disable();
				buttonFromEnum(ButtonsEnum.BTN_M_KICKOFF).enable();
			}        
			buttonFromEnum(ButtonsEnum.BTN_START).disable();
			buttonFromEnum(ButtonsEnum.BTN_STOP).activate();
			buttonFromEnum(ButtonsEnum.BTN_PARK).activate();
			if(StateMachine.GetCurrentGameState() == GameStateEnum.GS_OVERTIME)
			buttonFromEnum(ButtonsEnum.BTN_RESET).activate();
		}
		break;
		
	case GS_PENALTIES:
		refreshbutton_game_stopped();
		buttonAdisable();  //team A commands
		buttonBdisable();  //team B commands
		buttonCdisable();  //common commands
		
		bTeamAcmds[CMDID_TEAM_PENALTY].enable();
		bTeamBcmds[CMDID_TEAM_PENALTY].enable();
		
		if(StateMachine.setpiece)
		buttonFromEnum(StateMachine.setpiece_button).activate();
		buttonFromEnum(ButtonsEnum.BTN_START).enable();
		buttonFromEnum(ButtonsEnum.BTN_STOP).activate();
		buttonFromEnum(ButtonsEnum.BTN_PARK).disable();
		buttonFromEnum(ButtonsEnum.BTN_RESET).disable();

		bCommoncmds[CMDID_COMMON_DROP_BALL].disable();
		bCommoncmds[CMDID_COMMON_HALFTIME].enable();
		break;
		
	case GS_PENALTIES_ON:
		refreshbutton_game_on();
		break;
		
	case GS_ENDGAME:
		buttonAdisable();  //team A commands
		buttonBdisable();  //team B commands
		buttonCenable();   //common commands
		
		bCommoncmds[CMDID_COMMON_DROP_BALL].disable();
		bCommoncmds[CMDID_COMMON_HALFTIME].disable();
		bCommoncmds[CMDID_COMMON_RESET].activate();
		bCommoncmds[CMDID_COMMON_PARKING].activate();
		buttonCSTARTdisable();
		buttonCSTOPactivate();
		break;
		
	default:
		buttonAenable();  //team A commands
		buttonBenable();  //team B commands
		buttonCenable();  //common commands 
		buttonCSTOPactivate();
		break;
		
	}

	// The switches are enabled only on pre-game
	if(StateMachine.GetCurrentGameState() != GameStateEnum.GS_PREGAME)
	{
		for(int i = 0; i < bSlider.length; i++)
		bSlider[i].disable();
	}else{
		for(int i = 0; i < bSlider.length; i++)
		bSlider[i].enable();
	}

	// Update End Part / End Game button
	String endPartOrEndGame = "End Part";
	switch(StateMachine.GetCurrentGameState())
	{
	case GS_HALFTIME:
	case GS_GAMEON_H2: 
	case GS_GAMEON_H4:
	case GS_GAMESTOP_H2:
	case GS_GAMESTOP_H4:
		endPartOrEndGame = "End Game";
	}
	bCommoncmds[CMDID_COMMON_HALFTIME].Label = endPartOrEndGame; 
}

//*********************************************************************
// Start button has been pressed and game is now ON
public void refreshbutton_game_on()
{
	buttondisableAll();
	buttonCSTARTdisable();
	buttonCSTOPactivate();
}

//*********************************************************************
//
public void refreshbutton_game_stopped()
{

	if(bTeamAcmds[CMDID_TEAM_GOAL].isActive()) {
		buttonAdisable();
		buttonBdisable();
		buttonCdisable();    
		bTeamBcmds[CMDID_TEAM_KICKOFF].enable();
		bTeamBcmds[CMDID_TEAM_GOAL].disable();  
		bCommoncmds[CMDID_COMMON_HALFTIME].enable(); 
	}
	else if(bTeamBcmds[CMDID_TEAM_GOAL].isActive()) {
		buttonAdisable();
		buttonBdisable();
		buttonCdisable();    
		bTeamAcmds[CMDID_TEAM_KICKOFF].enable();    
		bTeamAcmds[CMDID_TEAM_GOAL].disable();    
		bCommoncmds[CMDID_COMMON_HALFTIME].enable();
	}
	else {
		if(!StateMachine.setpiece) {
			buttonA_setpieces_en();  //team A commands
			buttonB_setpieces_en();  //team B commands

			bCommoncmds[CMDID_COMMON_DROP_BALL].enable();
			bCommoncmds[CMDID_COMMON_HALFTIME].enable(); 
			bCommoncmds[CMDID_COMMON_PARKING].disable();
			bCommoncmds[CMDID_COMMON_RESET].disable();  
			bTeamAcmds[CMDID_TEAM_GOAL].enable();
			bTeamBcmds[CMDID_TEAM_GOAL].enable();
			buttonCSTARTdisable();            // Turn OFF START button  
		}
		else
		{
			bTeamAcmds[CMDID_TEAM_GOAL].disable();
			bTeamBcmds[CMDID_TEAM_GOAL].disable();
		}
		
		for(int i = CMDID_TEAM_REPAIR_OUT; i <= CMDID_TEAM_YELLOWCARD; i++)
		{
			if(!bTeamAcmds[i].isActive())
			bTeamAcmds[i].enable();

			if(!bTeamBcmds[i].isActive())
			bTeamBcmds[i].enable();
		}  
	}
	buttonCSTOPactivate();            // Turn ON STOP button

	if (teamA.numberOfPlayingRobots() < 3) bTeamAcmds[CMDID_TEAM_REPAIR_OUT].disable(); 
	if (teamB.numberOfPlayingRobots() < 3) bTeamBcmds[CMDID_TEAM_REPAIR_OUT].disable(); 
}

// ============================

//*********************************************************************
public void buttonA_setpieces_en()
{
	for (int i=CMDID_TEAM_FREEKICK; i <= CMDID_TEAM_PENALTY; i++)
	bTeamAcmds[i].enable();
	if (forceKickoff == true) bTeamAcmds[CMDID_TEAM_KICKOFF].enable();
}

//*********************************************************************
public void buttonB_setpieces_en()
{
	for (int i=CMDID_TEAM_FREEKICK; i <= CMDID_TEAM_PENALTY; i++)
	bTeamBcmds[i].enable();
	if (forceKickoff == true) bTeamBcmds[CMDID_TEAM_KICKOFF].enable();
}

//*********************************************************************
public void buttonAenable() {
	for (int i=0; i<bTeamAcmds.length; i++) {
		if (i>6 && bTeamAcmds[i].isActive()) ; //maintains goals+repair+cards
		else bTeamAcmds[i].enable();
	}
}
//*********************************************************************
public void buttonBenable() {
	for (int i=0; i<bTeamBcmds.length; i++) {
		if (i>6 && bTeamBcmds[i].isActive()) ; //maintains repair+cards
		else bTeamBcmds[i].enable();
	}
}

//*********************************************************************
public void buttonCenable() {
	for (int i=2; i<bCommoncmds.length; i++)
	bCommoncmds[i].enable();
}

//*********************************************************************
public void buttonAdisable() {
	for (int i=0; i <= CMDID_TEAM_PENALTY; i++){
		if (bTeamAcmds[i].isActive()){
			continue;
		}
		bTeamAcmds[i].disable();
	}
}

//*********************************************************************
public void buttonBdisable() {
	for (int i=0; i <= CMDID_TEAM_PENALTY; i++) {
		if (bTeamBcmds[i].isActive()) {
			continue;
		}
		bTeamBcmds[i].disable();
	}
}

//*********************************************************************
public void buttonAdisableAll(int index) {
	for (int i=0; i<bTeamAcmds.length; i++) {
		if (i != index) {
			bTeamAcmds[i].disable();
		}
	}
}

//*********************************************************************
public void buttonBdisableAll(int index) {
	for (int i=0; i<bTeamBcmds.length; i++){
		if (i != index) {
			bTeamBcmds[i].disable();
		}
	}
}

//*********************************************************************
public void buttonCdisable() {
	for (int i=2; i<bCommoncmds.length; i++) {
		if (StateMachine.GetCurrentGameState() != GameStateEnum.GS_PREGAME || i != CMDID_COMMON_RESET)
		if (bCommoncmds[i].isActive())continue;
		bCommoncmds[i].disable();
	}
}

public void buttondisableAll() {
	for (int i=0; i<bTeamAcmds.length; i++) 
	bTeamAcmds[i].disable();
	for (int i=0; i<bTeamBcmds.length; i++) 
	bTeamBcmds[i].disable();
	for (int i=2; i<bCommoncmds.length; i++)
	bCommoncmds[i].disable();
}

public void buttonCSTARTdisable() {
	bCommoncmds[0].disable();
}

//*********************************************************************
public void buttonCSTOPenable() {
	bCommoncmds[1].enable();
}

//*********************************************************************
public void buttonCSTOPactivate() {
	bCommoncmds[1].activate();
}

//*********************************************************************
public boolean isCSTOPactive() {
	return bCommoncmds[1].isActive();
}

//*********************************************************************
public boolean isCSTARTenabled() {
	return bCommoncmds[0].isEnabled();
}
//==============================================================================
//==============================================================================
class Robot {
	float guix, guiy;
	String state = "play"; //play , repair , yellow, doubleyellow , red
	StopWatch RepairTimer;
	StopWatch DoubleYellowTimer;

	Robot(float zx, float zy) {
		guix=zx; 
		guiy=zy;
		RepairTimer = new StopWatch(true, 0, false, false);
		DoubleYellowTimer = new StopWatch(true, 0, false, false);
	}

	//-------------------------------
	public void setState(String st) {
		state = st;
	}

	//-------------------------------
	public void reset() {
		this.state="play";
	}


	//-------------------------------
	public void updateUI(int c, boolean UIleft) {
		stroke(c); 
		strokeWeight(3);
		int rcolor=255;
		if (this.state.equals("repair")) rcolor=Config.robotRepairColor;
		if (this.state.equals("yellow")) rcolor=Config.robotYellowCardColor;  //yellow  
		if (this.state.equals("doubleyellow")) rcolor=Config.robotDoubleYellowCardColor;  //doubleyellow  
		if (this.state.equals("play")) rcolor=Config.robotPlayColor;  //white (very light-green)
		if (this.state.equals("red")) rcolor=Config.robotRedCardColor;  //red
		fill(rcolor);
		float tx=offsetRight.x + 106 + this.guix;
		float ty=offsetLeft.y + this.guiy;
		if (UIleft) tx=offsetLeft.x - 165 + this.guix;       
		ellipse(tx, ty, 42, 42);  
		fill(255);
		
		if(RepairTimer.getStatus() )
		{
			if (RepairTimer.getTimeMs() > 0)
			text(nf(PApplet.parseInt(RepairTimer.getTimeSec()), 2), tx, ty);
			else
			RepairTimer.stopTimer();
		}
		if(DoubleYellowTimer.getStatus() )
		{
			if (DoubleYellowTimer.getTimeMs() > 0)
			text(nf(PApplet.parseInt(DoubleYellowTimer.getTimeSec()), 2), tx, ty);
			else
			DoubleYellowTimer.stopTimer();
		}
	}

}
//==============================================================================
//==============================================================================
class ScoreClients
{
	//public MyServer scoreServer1;
	private static final boolean debug = false;
	public ArrayList<UDP> scoreClients = new ArrayList<UDP>();

	public ScoreClients(PApplet parent)
	{
		int numberOfClients = Config.scoreClientHosts.size();
		
		for(int i = 0; i < numberOfClients; i++)
		{
			scoreClients.add(new UDP(parent));
		}
	}

	// Sends an "event" type update message to the clients
	public void update_tEvent(String eventCode, String eventDesc, String team)
	{
		String msg = "{";
		msg += "\"type\": \"event\",";
		msg += "\"eventCode\": \"" + eventCode + "\",";
		msg += "\"eventDesc\": \"" + eventDesc + "\",";
		msg += "\"team\": \"" + team + "\"";
		msg += "}";
		msg += (char)0x00;
		
		if(debug)
		{
			println("Updating clients: " + eventCode + " (" + eventDesc + ")");
		}
		
		writeMsg(msg);
	}

	// Sends a "teams" type update message to the clients
	public void update_tTeams(String gamet,String gamerunt) {
		long startTime = System.currentTimeMillis();
		
		String snA=teamA.shortName;
		String lnA=teamA.longName;
		if (snA.length()>Config.maxShortName) snA=teamA.shortName.substring(0, Config.maxShortName);
		if (lnA.length()>Config.maxLongName) lnA=teamA.longName.substring(0, Config.maxLongName);     
		String snB=teamB.shortName;
		String lnB=teamB.longName;
		if (snB.length()>Config.maxShortName) snB=teamB.shortName.substring(0, Config.maxShortName);     
		if (lnB.length()>Config.maxLongName) lnB=teamB.longName.substring(0, Config.maxLongName);     

		String gamestateText = StateMachine.GetCurrentGameStateString();
		
		String teamA_robotState = "";
		String teamA_robotWaitTime = "";
		String teamA_world_json = "{}";
		if(teamA != null && teamA.worldstate_json != null)
		teamA_world_json = teamA.worldstate_json.toString();
		String teamB_robotState = "";
		String teamB_robotWaitTime = "";
		String teamB_world_json = "{}";
		if(teamB != null && teamB.worldstate_json != null)
		teamB_world_json = teamB.worldstate_json.toString();
		
		for(int i = 0; i < 5; i++){
			teamA_robotState += "\"" + teamA.r[i].state + "\"" + ((i==4)?"":",");
			teamB_robotState += "\"" + teamB.r[i].state + "\"" + ((i==4)?"":",");
			if (teamA.r[i].state.equals("doubleyellow") == true)
			teamA_robotWaitTime += teamA.r[i].DoubleYellowTimer.getTimeSec() + ((i==4)?"":",");
			else
			teamA_robotWaitTime += teamA.r[i].RepairTimer.getTimeSec() + ((i==4)?"":",");
			if (teamB.r[i].state.equals("doubleyellow") == true)
			teamB_robotWaitTime += teamB.r[i].DoubleYellowTimer.getTimeSec() + ((i==4)?"":",");
			else
			teamB_robotWaitTime += teamB.r[i].RepairTimer.getTimeSec() + ((i==4)?"":",");
		}
		
		String msg = "{";
		msg += "\"type\": \"teams\",";
		msg += "\"version\": \"" + MSG_VERSION + "\",";
		msg += "\"gameState\": " + StateMachine.GetCurrentGameState().getValue() + ",";
		msg += "\"gameStateString\": \"" + gamestateText + "\",";
		msg += "\"gameTime\": \"" + gamet + "\",";
		msg += "\"gameRunTime\": \"" + gamerunt + "\",";
		
		msg += "\"teamA\": {"; // Team A
		msg += "\"color\": \"" + hex(teamA.c,6) + "\",";
		msg += "\"shortName\": \"" + snA + "\",";
		msg += "\"longName\": \"" + lnA + "\",";
		msg += "\"score\": \"" + teamA.Score + "\",";
		msg += "\"robotState\": [" + teamA_robotState + "],";
		msg += "\"robotWaitTime\": [" + teamA_robotWaitTime + "],";
		msg += "\"worldState\": " + teamA_world_json;
		msg += "},"; // END Team A
		
		msg += "\"teamB\": {"; // Team B
		msg += "\"color\": \"" + hex(teamB.c,6) + "\",";
		msg += "\"shortName\": \"" + snB + "\",";
		msg += "\"longName\": \"" + lnB + "\",";
		msg += "\"score\": \"" + teamB.Score + "\",";
		msg += "\"robotState\": [" + teamB_robotState + "],";
		msg += "\"robotWaitTime\": [" + teamB_robotWaitTime + "],";
		msg += "\"worldState\": " + teamB_world_json;
		msg += "}"; // END Team B
		
		msg += "}";
		
		msg += (char)0x00;
		
		writeMsg(msg);
		updateScoreClientslasttime=System.currentTimeMillis();
		
		//logMessage("Send to score clients " + (updateScoreClientslasttime-startTime) + " ms");
	}

	public int clientCount()
	{
		return Config.scoreClientHosts.size();
	}

	public void stopServer()
	{
		for(int i = 0; i < clientCount(); i++)
		{
			scoreClients.get(i).close();
		}
	}

	public void writeMsg(String message)
	{
		// Write message to all clients
		for(int i = 0; i < clientCount(); i++)
		scoreClients.get(i).send(message, Config.scoreClientHosts.get(i), Config.scoreClientPorts.get(i));
	}

}
static class StateMachine
{

	private static boolean needUpdate = false; 
	private static boolean btnOn = false;
	private static ButtonsEnum btnCurrent = ButtonsEnum.BTN_ILLEGAL;
	private static ButtonsEnum btnPrev = ButtonsEnum.BTN_ILLEGAL;
	public static GameStateEnum gsCurrent = GameStateEnum.GS_PREGAME;
	private static GameStateEnum gsPrev = GameStateEnum.GS_ILLEGAL;

	public static boolean setpiece = false;
	public static boolean setpiece_cyan = false;
	public static ButtonsEnum setpiece_button = null;

	public static boolean firstKickoffCyan = true;

	public static void Update(ButtonsEnum click_btn, boolean on) //If on==True then active
	{
		btnCurrent = click_btn;
		btnOn = on;
		needUpdate = true; 
		StateMachineRefresh();
	}

	//************************************************************************
	// Basic state machine main refresh
	//************************************************************************
	private static void StateMachineRefresh()
	{
		GameStateEnum nextGS = GameStateEnum.newInstance(gsCurrent);
		GameStateEnum saveGS = GameStateEnum.newInstance(gsCurrent);
		
		// Check popup response when popup is ON
		if(Popup.hasNewResponse())
		{
			switch(Popup.getType())
			{
			case POPUP_RESET:
				{
					if(Popup.getResponse().equals("yes"))
					{
						send_event_v2(cCommcmds[CMDID_COMMON_RESET], Commcmds[CMDID_COMMON_RESET], null);
						Popup.close();
						gsCurrent = GameStateEnum.GS_RESET;            // Game over
						needUpdate = true;
						reset();
						Popup.show(PopupTypeEnum.POPUP_WAIT, MSG_WAIT, 0, 0, 0, 24, 380, 100);
						return;
					}
					break;
				}
				
			case POPUP_ENDPART:
				{
					if(Popup.getResponse().equals("yes"))
					{
						gsCurrent = SwitchGamePart();
						gsPrev = saveGS;
						mainWatch.resetStopWatch();
						playTimeWatch.resetStopWatch();
						SetPieceDelay.resetStopWatch();
						SetPieceDelay.stopTimer();

						if (bCommoncmds[CMDID_COMMON_HALFTIME].Label.equals("End Game"))
						send_event_v2(cCommcmds[CMDID_COMMON_ENDGAME], Commcmds[CMDID_COMMON_ENDGAME], null);
						else
						send_event_v2(cCommcmds[CMDID_COMMON_HALFTIME], Commcmds[CMDID_COMMON_HALFTIME], null);            
					}
					break;
				}
				
			case POPUP_TEAMSELECTION:
				{
					Team t = null;
					if(Popup.getResponse().equals("cyan"))
					{
						Log.logMessage("Connection from " + connectingClient.ip() + " accepted - Cyan");
						t = teamA;
					}else{
						Log.logMessage("Connection from " + connectingClient.ip() + " accepted - Magenta");
						t = teamB;
					}
					
					if(t != null)
					t.teamConnected(teamselect);          
					break;
				}
				
			case POPUP_REPAIRL:
				{
					if(Popup.getResponse().equals("1")) teamA.nOfRepairs = 1; 
					if(Popup.getResponse().equals("2")) teamA.nOfRepairs = 2;
					if(Popup.getResponse().equals("3")) teamA.nOfRepairs = 3;
					break;
				}
				
			case POPUP_REPAIRR:
				{
					if(Popup.getResponse().equals("1")) teamB.nOfRepairs = 1; 
					if(Popup.getResponse().equals("2")) teamB.nOfRepairs = 2;
					if(Popup.getResponse().equals("3")) teamB.nOfRepairs = 3;
					break;
				}
			}      
			needUpdate = false;
			Popup.close();
			return;
		}
		
		if(needUpdate)
		{
			// Goal buttons
			int add = (btnOn ? +1 : -1);
			int i;
			
			if(btnCurrent.isGoal())
			{
				if(btnCurrent.isCyan()) teamA.Score+=add;
				else teamB.Score+=add;
			}
			else if(btnCurrent.isReset())
			{
				Popup.show(PopupTypeEnum.POPUP_RESET, MSG_RESET, 1, 0, 2, 24, 380, 200);
				needUpdate = false;
				return;
			}
			else if(btnCurrent.isEndPart())
			{
				Popup.show(PopupTypeEnum.POPUP_ENDPART, MSG_HALFTIME, 1, 0, 2, 24, 380, 200);
				needUpdate = false;
				return;
			}
			else if(btnCurrent.isRepair())
			{
				if(btnCurrent.isCyan()){
					teamA.newRepair=btnOn;
					if (btnOn) {
						i = teamA.numberOfPlayingRobots() - 2;
						println (i);
						if (i == 3)
						Popup.show(PopupTypeEnum.POPUP_REPAIRL, MSG_REPAIR, 5, 6, 7, 24, 380, 200);
						else if(i == 2)
						Popup.show(PopupTypeEnum.POPUP_REPAIRL, MSG_REPAIR, 5, 6, 0, 24, 380, 200);		  
					}
				}
				else {
					teamB.newRepair=btnOn;
					if (btnOn) {
						i = teamB.numberOfPlayingRobots() - 2;
						println (i);
						if (i == 3)
						Popup.show(PopupTypeEnum.POPUP_REPAIRR, MSG_REPAIR, 5, 6, 7, 24, 380, 200);		  
						else if(i == 2)
						Popup.show(PopupTypeEnum.POPUP_REPAIRR, MSG_REPAIR, 5, 6, 0, 24, 380, 200);		  
					}
				}
			}
			else if(btnCurrent.isRed())
			{
				if(btnCurrent.isCyan())
				teamA.newRedCard=btnOn;
				else
				teamB.newRedCard=btnOn;
			}
			else if(btnCurrent.isYellow())
			{
				Team t = teamA;
				if(!btnCurrent.isCyan())
				t = teamB;
				
				if (t.YellowCardCount==1)
				t.newDoubleYellow = btnOn;
				else
				t.newYellowCard = btnOn;
			}
			else if(btnCurrent.isStop())
			{
				SetPieceDelay.resetStopWatch();
				SetPieceDelay.stopTimer();
				forceKickoff = false; 
			}
			
			println ("Current: " + gsCurrent);
			switch(gsCurrent)
			{
				
				// PRE-GAME and Half Times
			case GS_PREGAME:
			case GS_HALFTIME:
			case GS_OVERTIME:
			case GS_HALFTIME_OVERTIME:
				if(btnCurrent == ButtonsEnum.BTN_START)
				{
					mainWatch.resetStopWatch();
					playTimeWatch.resetStopWatch(); 
					SetPieceDelay.resetStopWatch();	
					SetPieceDelay.stopTimer();			
					nextGS = SwitchRunningStopped();
					switch(nextGS)
					{
					case GS_GAMEON_H1: send_to_basestation(COMM_FIRST_HALF + ""); break;
					case GS_GAMEON_H2: send_to_basestation(COMM_SECOND_HALF + ""); break;
					case GS_GAMEON_H3: send_to_basestation(COMM_FIRST_HALF_OVERTIME + ""); break;
					case GS_GAMEON_H4: send_to_basestation(COMM_SECOND_HALF_OVERTIME + ""); break;
					}
				}
				else if(btnCurrent == ButtonsEnum.BTN_STOP)
				{
					if(setpiece)
					ResetSetpiece();
				}
				else if(btnCurrent == ButtonsEnum.BTN_C_KICKOFF)
				{
					// Save first kickoff
					if(gsCurrent == GameStateEnum.GS_PREGAME)
					firstKickoffCyan = true;
					SetSetpiece(true, btnCurrent);
				}
				else if(btnCurrent == ButtonsEnum.BTN_M_KICKOFF)
				{
					if(gsCurrent == GameStateEnum.GS_PREGAME)
					firstKickoffCyan = false;
					SetSetpiece(false, btnCurrent);
				}
				
				break;
				
			case GS_GAMESTOP_H1:
			case GS_GAMESTOP_H2:
			case GS_GAMESTOP_H3:
			case GS_GAMESTOP_H4:
				if(btnCurrent.isSetPiece())
				SetSetpiece(btnCurrent.isCyan(), btnCurrent);
				else if(btnCurrent.isStart()){
					nextGS = SwitchRunningStopped();
				}
				else if(btnCurrent.isStop()) 
				{
					ResetSetpiece();
					SetPieceDelay.resetStopWatch();
					SetPieceDelay.stopTimer();
				}
				else if(btnCurrent.isEndPart())
				nextGS = SwitchGamePart();
				break;
				
			case GS_GAMEON_H1:
			case GS_GAMEON_H2:
			case GS_GAMEON_H3:
			case GS_GAMEON_H4:
				if(setpiece)
				ResetSetpiece();
				
				if(btnCurrent == ButtonsEnum.BTN_STOP)		// Button stop pressed
				{
					nextGS = SwitchRunningStopped();
				}
				break;
				
			case GS_PENALTIES:
				if(btnCurrent.isSetPiece())                       // Kick Off either, Penalty either, DropBall
				SetSetpiece(btnCurrent.isCyan(), btnCurrent);
				else if(btnCurrent.isStop()) {
					ResetSetpiece();
					SetPieceDelay.resetStopWatch();
					SetPieceDelay.stopTimer();
				}
				else if(btnCurrent.isEndPart())
				nextGS = SwitchGamePart();
				else if(btnCurrent.isStart())
				nextGS = SwitchRunningStopped();
				break;
				
			case GS_PENALTIES_ON:
				if(setpiece)
				ResetSetpiece();
				if(btnCurrent.isStop()){
					SetPieceDelay.resetStopWatch();	
					SetPieceDelay.stopTimer();			
					nextGS = SwitchRunningStopped();
				}
				break;
				//<>//
			case GS_ENDGAME:
				break;
				
			case GS_RESET:
				saveData();
				break;
			}
			
			if(nextGS != null)        //What to do when there is a new game state
			{
				
				gsCurrent = nextGS;
				gsPrev = saveGS;
				
				if(gsCurrent.getValue() != gsPrev.getValue())
				{
					teamA.checkflags();
					teamB.checkflags();
				}
			}
			
			btnPrev = btnCurrent;      
			needUpdate = false;
		}
	}

	//************************************************************************
	// 
	//************************************************************************
	private static GameStateEnum SwitchGamePart()
	{
		switch(gsCurrent)
		{
		case GS_GAMESTOP_H1: return GameStateEnum.GS_HALFTIME;
		case GS_GAMESTOP_H2: return GameStateEnum.GS_OVERTIME;
		case GS_GAMESTOP_H3: return GameStateEnum.GS_HALFTIME_OVERTIME;
		case GS_GAMESTOP_H4: return GameStateEnum.GS_PENALTIES;
		case GS_PENALTIES: return GameStateEnum.GS_ENDGAME;
		}
		
		return null;
	}

	//************************************************************************
	// 
	//************************************************************************
	private static GameStateEnum SwitchRunningStopped()
	{
		switch(gsCurrent)
		{
		case GS_GAMEON_H1: return GameStateEnum.GS_GAMESTOP_H1;
		case GS_GAMEON_H2: return GameStateEnum.GS_GAMESTOP_H2;
		case GS_GAMEON_H3: return GameStateEnum.GS_GAMESTOP_H3;
		case GS_GAMEON_H4: return GameStateEnum.GS_GAMESTOP_H4;
			
		case GS_PREGAME:
		case GS_GAMESTOP_H1:
			return GameStateEnum.GS_GAMEON_H1;
		case GS_HALFTIME:
		case GS_GAMESTOP_H2:
			return GameStateEnum.GS_GAMEON_H2;
		case GS_OVERTIME:
		case GS_GAMESTOP_H3:
			return GameStateEnum.GS_GAMEON_H3;
		case GS_HALFTIME_OVERTIME:
		case GS_GAMESTOP_H4:
			return GameStateEnum.GS_GAMEON_H4;
			
		case GS_PENALTIES: return GameStateEnum.GS_PENALTIES_ON;
		case GS_PENALTIES_ON: return GameStateEnum.GS_PENALTIES;
		}
		
		return null;
	}

	//************************************************************************
	// 
	//************************************************************************
	private static void ResetSetpiece()
	{
		setpiece = false;
	}

	//************************************************************************
	// 
	//************************************************************************
	private static void SetSetpiece(boolean cyan, ButtonsEnum btn)
	{
		setpiece = true;
		setpiece_cyan = cyan;
		setpiece_button = btn;
	}

	//************************************************************************
	// 
	//************************************************************************
	public static GameStateEnum GetCurrentGameState()
	{
		return gsCurrent;
	}

	//************************************************************************
	// 
	//************************************************************************
	public static String GetCurrentGameStateString()
	{
		if(gsCurrent != null)
		return gsCurrent.getName();
		else
		return "";
	}

	//************************************************************************
	// Reset after end of game
	//************************************************************************
	public static void reset()
	{
		try {
			send_to_basestation("" + COMM_RESET);
			buttonFromEnum(ButtonsEnum.BTN_PARK).disable();
			btnCurrent = ButtonsEnum.BTN_ILLEGAL;
			btnPrev = ButtonsEnum.BTN_ILLEGAL;
			gsCurrent = GameStateEnum.GS_PREGAME;
			gsPrev = GameStateEnum.GS_ILLEGAL;
			
			teamA.reset();
			teamB.reset();        
			teamA.resetname();
			teamB.resetname();        
			mainWatch.resetStopWatch();
			playTimeWatch.resetStopWatch();
			SetPieceDelay.resetStopWatch();
			SetPieceDelay.stopTimer();
		} catch(Exception e) {}
	}

	//************************************************************************
	// Save data on reset
	//************************************************************************
	public static void saveData()
	{
		try {

			LogMerger merger = new LogMerger(Log.getTimedName());
			merger.merge();		  
			Log.createLog();
			BaseStationServer.stop();
			BaseStationServer = new MyServer(mainApplet, Config.basestationServerPort);
		} catch(Exception e) {}

	}

	//************************************************************************
	// 
	//************************************************************************
	public static boolean isHalf()
	{
		return is1stHalf() || is2ndHalf() || is3rdHalf() || is4thHalf();
	}

	public static boolean isPreGame()
	{
		return gsCurrent == GameStateEnum.GS_PREGAME;
	}

	public static boolean is1stHalf()
	{
		return gsCurrent == GameStateEnum.GS_GAMESTOP_H1 || gsCurrent == GameStateEnum.GS_GAMEON_H1;
	}

	public static boolean is2ndHalf()
	{
		return gsCurrent == GameStateEnum.GS_GAMESTOP_H2 || gsCurrent == GameStateEnum.GS_GAMEON_H2;
	}

	public static boolean is3rdHalf()
	{
		return gsCurrent == GameStateEnum.GS_GAMESTOP_H3 || gsCurrent == GameStateEnum.GS_GAMEON_H3;
	}

	public static boolean is4thHalf()
	{
		return gsCurrent == GameStateEnum.GS_GAMESTOP_H4 || gsCurrent == GameStateEnum.GS_GAMEON_H4;
	}

	public static boolean isInterval() 
	{
		return gsCurrent == GameStateEnum.GS_HALFTIME || gsCurrent == GameStateEnum.GS_OVERTIME || gsCurrent == GameStateEnum.GS_HALFTIME_OVERTIME || gsCurrent == GameStateEnum.GS_GAMESTOP_H4 || gsCurrent == GameStateEnum.GS_PENALTIES;
	}

}

//************************************************************************
// 
//************************************************************************
public void StateMachineCheck() {
	StateMachine.StateMachineRefresh();
}
// Processing mouse'event
public void mousePressed() {
	if (!Popup.isEnabled()) {
		//sliders
		boolean refreshslider = false;
		int pos = -1;
		
		for (int i=0; i<4; i++)
		if (bSlider[i].mouseover()) { bSlider[i].toogle(); refreshslider=true; pos=i; break;}
		if (refreshslider) {    
			setbooleansfrombsliders();
			//if (pos==0) screenlog("Testmode "+(TESTMODE?"enabled":"disabled"));
			if (pos==1) Log.screenlog("Log "+(Log.enable?"enabled":"disabled"));
			if (pos==2) Log.screenlog("Remote "+(REMOTECONTROLENABLE?"enabled":"disabled"));
		}
		
		//common commands
		for (int i=0; i<bCommoncmds.length; i++) {
			if (bCommoncmds[i].isEnabled()) {
				bCommoncmds[i].checkhover();
				if (bCommoncmds[i].HOVER==true) { 
					buttonEvent('C', i); 
					break;
				}
			}
		}
		
		//team commands
		for (int i=0; i<bTeamAcmds.length; i++) {
			if (bTeamAcmds[i].isEnabled()) {
				bTeamAcmds[i].checkhover();
				if (bTeamAcmds[i].HOVER==true) { 
					buttonEvent('A', i); 
					break;
				}
			}
			if (bTeamBcmds[i].isEnabled()) {
				bTeamBcmds[i].checkhover();
				if (bTeamBcmds[i].HOVER==true) { 
					buttonEvent('B', i); 
					break;
				}
			}
		}
		
	}
	else {//POPUP
		Popup.check(true);
	}
}

// Processing mouse'event
public void mouseMoved() {
	if (!Popup.isEnabled()) {
		for (int i=0; i<bTeamAcmds.length; i++) {
			if (bTeamAcmds[i].isEnabled()) bTeamAcmds[i].checkhover();
			if (bTeamBcmds[i].isEnabled()) bTeamBcmds[i].checkhover();
		}  
		for (int i=0; i<bCommoncmds.length; i++)
		if (bCommoncmds[i].isEnabled()) bCommoncmds[i].checkhover();  
	} 
	else {  				//check popup
		Popup.check(false);
	}
}

// Processing key'event
public void keyPressed() {

	if (key == ESC){
		key = 0; 		//disable quit on ESC
		// Close popup
		if(Popup.isEnabled()) 
		Popup.close();
	}
	if (key == 32){
		key = 0; 		
		buttonEvent('C', ButtonsEnum.BTN_STOP.getValue());
	}
	if (key == CODED) {
		if (keyCode == ALT) altK = true;
		key = 0;
	}
	if (altK == true && (key == 'r' || key == 'R')){
		key = 0;
		buttonFromEnum(ButtonsEnum.BTN_RESET).enable();
		buttonEvent('C', ButtonsEnum.BTN_RESET.getValue());		
		buttonFromEnum(ButtonsEnum.BTN_RESET).disable();
		buttonEvent('C', ButtonsEnum.BTN_STOP.getValue()); 
	}
	if (altK == true && (key == 'k' || key == 'K')){
		key = 0;
		forceKickoff = true;
	}
	if (key == 'H') {
		key = 0;
		Popup.show(PopupTypeEnum.POPUP_HELP, MSG_HELP, 8, 0, 0, 20, 440, 240);
	}
	key = 0;

}

public void keyReleased() {
	if (key == CODED) {
		if (keyCode == ALT) altK = false; 
		key = 0;
	}	
}
class Team {
	String shortName;  //max 8 chars
	String longName;  //max 24 chars
	String unicastIP, multicastIP;
	int c=(0xff000000);
	boolean isCyan;  //default: cyan@left
	boolean newYellowCard, newRedCard, newRepair, newDoubleYellow, newPenaltyKick, newGoal; // Pending commands, effective only on gamestate change
	int Score, RedCardCount, YellowCardCount, DoubleYellowCardCount, PenaltyCount;
	public int RepairCount;
	public int nOfRepairs;
	int tableindex=0;
	org.json.JSONObject worldstate_json;
	String wsBuffer;
	Robot[] r=new Robot[5];

	File logFile;
	PrintWriter logFileOut;
	Client connectedClient;
	boolean firstWorldState;
	
	Team(int c, boolean uileftside) {
		this.c=c;
		this.isCyan=uileftside;
		//robots
		float x=0, y=64; 
		r[0]=new Robot(x, y);
		r[1]=new Robot(x+56, y);
		r[2]=new Robot(x, y + 56);
		r[3]=new Robot(x+56, y + 56);
		r[4]=new Robot(x+28, y + 112);

		this.reset();
	}

	//===================================

	public void resetname(){
		if (this.isCyan) {
			this.shortName=Config.defaultCyanTeamShortName;
			this.longName=Config.defaultCyanTeamLongName;
		}
		else {
			this.shortName=Config.defaultMagentaTeamShortName;
			this.longName=Config.defaultMagentaTeamLongName;
		}
	}

	public void logWorldstate(String teamWorldstate, int ageMs){
		if(logFileOut == null)
		return;

		if(firstWorldState) {
			logFileOut.println("[");    // Start of JSON array
			firstWorldState = false;
		}else{
			logFileOut.println(",");    // Separator for the new JSON object
		}

		logFileOut.print("{");
		logFileOut.print("\"teamName\": \"" + shortName + "\",");
		logFileOut.print("\"timestamp\": " + (System.currentTimeMillis() - ageMs) + ",");
		logFileOut.print("\"gametimeMs\": " + mainWatch.getTimeMs() + ",");
		logFileOut.print("\"worldstate\": " + teamWorldstate);
		logFileOut.print("}");

	}

	public void reset() {
		if(logFileOut != null) {
			logFileOut.println("]");    // End JSON array
			logFileOut.close();
		}

		logFileOut = null;
		logFile = null;

		this.resetname();
		this.worldstate_json = null;
		this.wsBuffer = "";
		this.Score=0; 
		this.RepairCount=0;
		this.nOfRepairs = 1;
		this.RedCardCount=0;
		this.YellowCardCount=0;
		this.DoubleYellowCardCount=0;
		this.PenaltyCount=0;
		this.newYellowCard=false;
		this.newRedCard=false;
		this.newRepair=false;
		this.newDoubleYellow=false;
		this.newPenaltyKick=false;
		for (int i=0; i<5; i++)
		r[i].reset();

		if(this.connectedClient != null && this.connectedClient.active())
		this.connectedClient.stop();
		this.connectedClient = null;
		this.firstWorldState = true;
	}

	// Function called when team connects and is accepted
	public void teamConnected(TableRow teamselect){
		shortName=teamselect.getString("shortname8");
		longName=teamselect.getString("longame24");
		unicastIP = teamselect.getString("UnicastAddr");
		multicastIP = teamselect.getString("MulticastAddr");


		if(connectedClient != null)
		BaseStationServer.disconnect(connectedClient);

		connectedClient = connectingClient;
		connectingClient.write(COMM_WELCOME);
		connectingClient = null;

		if(this.logFile == null || this.logFileOut == null)
		{
			this.logFile = new File(mainApplet.dataPath("tmp/" + Log.getTimedName() + "." + (isCyan?"A":"B") + ".msl"));
			try{
				this.logFileOut = new PrintWriter(new BufferedWriter(new FileWriter(logFile, true)));
			}catch(IOException e){ }
		}
	}


	//*******************************************************************
	//*******************************************************************
	public void repair_timer_start(int rpCount) { 
		r[rpCount].RepairTimer.startTimer(Config.repairPenalty_ms);

		if (isCyan)
		println("Repair Cyan "+(rpCount+1)+" started!");
		else
		println("Repair Magenta "+(rpCount+1)+" started!");
	}

	//*******************************************************************
	//*******************************************************************
	public void repair_timer_check(int rpCount) {
		if (r[rpCount].RepairTimer.getStatus())
		{
			if (r[rpCount].RepairTimer.getTimeMs() > 0)
			{
				if (StateMachine.isInterval()) {
					r[rpCount].RepairTimer.resetStopWatch();
					println("Repair "+(rpCount+1)+" reseted!");
				}
			}
			else
			{
				r[rpCount].RepairTimer.resetStopWatch();
				RepairCount--;
				println("Repair OUT: "+shortName+":"+(rpCount+1)+" @"+(isCyan?"left":"right"));
				r[rpCount].setState("play");
			}
		}
		else
		r[rpCount].setState("play");	
	}

	//*******************************************************************
	public void double_yellow_timer_start(int rpCount) {
		r[rpCount].DoubleYellowTimer.startTimer(Config.doubleYellowPenalty_ms);
		if (isCyan)
		println("Double Yellow Cyan "+(rpCount+1)+" started!");
		else
		println("Double Yellow Magenta "+(rpCount+1)+" started!");
	}

	//*******************************************************************
	public void double_yellow_timer_check(int rpCount) {
		if (r[rpCount].DoubleYellowTimer.getStatus())
		{
			if (r[rpCount].DoubleYellowTimer.getTimeMs() == 0)
			{
				r[rpCount].DoubleYellowTimer.resetStopWatch();
				DoubleYellowCardCount--;
				println("Double Yellow end: "+shortName+":"+(rpCount+1)+" @"+(isCyan?"left":"right"));
				r[rpCount].setState("play");
			}
		}
		else
		r[rpCount].setState("play");
	}

	//*******************************************************************
	public void checkflags() {
		int i;  
		if (this.newRepair) {
			while (this.nOfRepairs > 0) {
				for (i = 0; i < 3; i++) if (this.r[i].state == "play") break;
				if (i < 3) {
					this.repair_timer_start(i);
					this.RepairCount++;
					this.r[i].setState("repair");	  
					// Hack: send command only on game change
				}
				this.nOfRepairs --;
			}
			if(this.isCyan) event_message_v2(ButtonsEnum.BTN_C_REPAIR, true);
			else event_message_v2(ButtonsEnum.BTN_M_REPAIR, true);
			this.newRepair=false;
			this.nOfRepairs = 1;
		}

		if (this.newYellowCard) {
			this.YellowCardCount = 1;
			this.r[4].setState("yellow");	  
			this.newYellowCard = false;

			// Hack: send command only on game change
			if(this.isCyan) event_message_v2(ButtonsEnum.BTN_C_YELLOW, true);
			else event_message_v2(ButtonsEnum.BTN_M_YELLOW, true);
		}

		if (this.newRedCard) {
			this.RedCardCount++;
			for (i = 3; i >= 0; i--) if (this.r[i].state == "play") break;
			if (i >= 0 ) {
				this.r[i].setState("red");	  

				// Hack: send command only on game change
				if(this.isCyan) event_message_v2(ButtonsEnum.BTN_C_RED, true);
				else event_message_v2(ButtonsEnum.BTN_M_RED, true);
			}
			this.newRedCard = false;
		}

		if (this.newDoubleYellow) {
			for (i = 3; i >= 0; i--) if (this.r[i].state == "play") break;
			if (i >= 0 ) {
				this.double_yellow_timer_start(i);
				this.r[i].setState("doubleyellow");	  
				this.r[4].setState("play");	  
				this.DoubleYellowCardCount++;
				this.YellowCardCount = 0;

				if(this.isCyan) send_event_v2(""+COMM_DOUBLE_YELLOW_CYAN, "Double Yellow", this);
				else send_event_v2(""+COMM_DOUBLE_YELLOW_MAGENTA, "Double Yellow", this);
			}
			this.newDoubleYellow = false;
		}

		if (this.newPenaltyKick) {
			this.PenaltyCount++;
			this.newPenaltyKick=false;
		}
	}

	public int numberOfPlayingRobots()
	{
		int i, count;
		for (i = 0, count = 0; i < 5; i++)
		if (this.r[i].state.equals("play") || this.r[i].state.equals("yellow")) count++;
		return count;
	}

	//*******************************************************************
	//*******************************************************************

	public void updateUI() {
		if(connectedClient != null && !connectedClient.active())
		{
			println("Connection to team \"" + longName + "\" dropped.");
			Log.logMessage("Team " + shortName + " dropped");
			BaseStationServer.disconnect(connectedClient);
			resetname();
			connectedClient = null;
		}

		//team names
		String sn=shortName;
		String ln=longName;
		if (sn.length()>Config.maxShortName) sn=shortName.substring(0, Config.maxShortName);
		if (ln.length()>Config.maxLongName) ln=longName.substring(0, Config.maxLongName);
		rectMode(CENTER);
		fill(255);

		textFont(teamFont);
		textAlign(CENTER, CENTER);    
		if (isCyan) text(sn, 163, 50);
		else text(sn, 837, 50);

		textFont(panelFont);
		if (isCyan) text(ln, 163, 90);
		else text(ln, 837, 90);

		for (int i=0; i < 4; i++) {
			r[i].RepairTimer.updateStopWatch();
			r[i].DoubleYellowTimer.updateStopWatch();
		}

		for (int i=0; i < 4; i++) {
			if (r[i].state == "repair") repair_timer_check(i);
		}

		for (int i=0; i < 4; i++) {
			if (r[i].state == "doubleyellow") double_yellow_timer_check(i);
		}    

		for (int i=0; i<5; i++)
		r[i].updateUI(c,isCyan);

		textAlign(LEFT, BOTTOM);
		textFont(debugFont);
		fill(0xffffff00);
		textLeading(20);
		String ts="Goals."+this.Score+" Penalty:"+this.PenaltyCount+"\nYellow:"+this.YellowCardCount+" Red:"+this.RedCardCount+"\nRepair:"+this.RepairCount+" 2xYellow:"+this.DoubleYellowCardCount;
		if (isCyan) text(ts, 40, height-18);
		else text(ts, width - 190, height-18);
	}

	//*******************************************************************
	public boolean IPBelongs(String clientipstr){
		if(this.unicastIP == null)
		return false;

		String[] iptokens;

		if (!clientipstr.equals("0:0:0:0:0:0:0:1")) {
			iptokens=split(clientipstr,'.');
			if (iptokens!=null) clientipstr=iptokens[0]+"."+iptokens[1]+"."+iptokens[2]+".*";
		}

		return this.unicastIP.equals(clientipstr);
	}
}



class TeamTableBuilder {
	private String teamTableSettingsNames[] = {"UnicastAddr", "MulticastAddr", "Team", "longame24", "shortname8"};
	private JSONArray teamSettings;

	TeamTableBuilder(String filename) {
		teamSettings = loadJSONArray(filename);
	} 

	private Table makeTable() {
		Table table = new Table();

		for (String settingsName: teamTableSettingsNames) {
			table.addColumn(settingsName);
		}

		return table;
	}

	private void addTeamSettingToRow(TableRow row, JSONObject teamSetting) {
		for (String settingsName: teamTableSettingsNames) {
			row.setString(settingsName, teamSetting.getString(settingsName));
		}
	}

	public Table build() {
		Table table = makeTable();

		for (int i = 0; i < teamSettings.size(); i++) {
			TableRow newRow = table.addRow();

			JSONObject teamSetting = teamSettings.getJSONObject(i);

			addTeamSettingToRow(newRow, teamSetting);
		}

		return table;
	} 
}
public class StopWatch {
	
	// StopWatch fields
	private long oldTime;
	private long deltaTime;
	private boolean countOffTime;        // if true StopWatch keeps incrementing/decrementing while game is stoped
	private boolean status;              // StopWatch ON / OFF status
	private boolean isTimer;             // when true indicates that the stopWatch works as a Timer
	private long currentTimeMs;          // in ms
	private long currentTimeSec;         // in seconds (seeling of currentTimeMs / 1000)
	
	// StopWatch constructor
	// Parameters:
	//        startValue - expressed in seconds, can be greater or equal to zero
	//        coT -a boolean that determines if time counting is continuous or stops during game stoppage time
	//             see countOffTime
	//      isTimer - Bollean that, when true indicates that the stopWatch works as a Timer
	//        startUp - Boolean. If true the stopWatch starts imediatly
	public StopWatch(boolean isTimer, long startValue, boolean cOT, boolean startUp) 
	{
		oldTime = System.currentTimeMillis();  
		deltaTime = 0;
		countOffTime = cOT;
		currentTimeSec = startValue;
		currentTimeMs =  startValue * 1000;
		status = startUp;
		this.isTimer = isTimer;
	}
	
	// StopWatch Methods

	public void updateStopWatch(){                // This method should be called once every draw()
		long t = System.currentTimeMillis();
		this.deltaTime = t - oldTime;
		oldTime = t;
		adjustValues();
	}
	
	private void adjustValues()
	{
		if (isTimer)
		{
			if (currentTimeMs > 0)
			{
				if (status && (countOffTime || StateMachine.gsCurrent.isRunning()))
				{
					currentTimeMs = (deltaTime > currentTimeMs) ? 0 : currentTimeMs - deltaTime;
					currentTimeSec = (currentTimeMs > 0) ? 1 + (currentTimeMs/1000) : 0;
				}
			}
		}
		else
		{
			if (status && (countOffTime || StateMachine.gsCurrent.isRunning()))
			{
				currentTimeMs += deltaTime;
				currentTimeSec = (currentTimeMs/1000);
			}
		}
	}

	public void resetStopWatch()
	{
		currentTimeMs = 0;
		currentTimeSec = 0;
	}

	public void stopSW()
	{
		status = false;
	}

	public void stopTimer()
	{
		status = false;
	}

	public void startSW()
	{
		status = true;
	}    
	
	public void startTimer(long timeMs)
	{
		status = true;
		currentTimeMs =  timeMs;
		currentTimeSec = PApplet.parseInt(timeMs / 1000);
	}
	
	public long getTimeMs()
	{
		return currentTimeMs;
	}
	
	public long getTimeSec()
	{
		return currentTimeSec;
	}
	
	public boolean getStatus()
	{
		return status;
	}
}
    public void settings() { 	size(1000, 680); }
    static public void main(String[] passedArgs) {
        String[] appletArgs = new String[] { "mslrb2015" };
        if (passedArgs != null) {
          PApplet.main(concat(appletArgs, passedArgs));
        } else {
          PApplet.main(appletArgs);
        }
    }
}
