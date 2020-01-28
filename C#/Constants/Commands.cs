using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Constants
{
    public enum Commands
    {
        #region Commandes générales
        #pragma warning disable CS1591

        Unknown = 0,

        // Commandes de contrôle de flux

        Time = 128,
        RobotLoopback = 129,
        ClientLoopback = 130,
        AskDisconnection = 131,
        Disconnected = 132,
        ResetEmbedded = 133,
        RobotWelcome = 134,
        RobotMainInfos = 135,
        RobotAccepted = 136,
        RobotRefused = 137,
        RobotInfos = 138,
        ConfigurationInfo = 139,

        // Setters de variables embarquées

        SetXYSpeed = 200,
        SetMotorSpeed = 201,
        SetMotorSpeed_Each = 202,
        SetKu = 203,
        SetKu_Each = 204,
        SetPu = 205,
        SetPu_Each = 206,
        SetBaudrate = 207,
        SetLEDState = 208,
        SetLEDState_Each = 209,
        SetAsservMode = 210,
        SetAsservMode_Each = 211,
        SetX = 212,
        SetY = 213,
        SetTheta = 214,
        SetXYTheta = 215,
        SetTrajectoryParameters = 216,
        SetAsservissementEnable = 217,
        SetServoEnable = 218,
        SetCarteTirAlim = 219,
        FieldLinesFound = 220,

        // Getters et Askers de variables embarquées


        MotorSpeedConsigne = 402,
        MotorSpeedConsigne_Each = 403,
        Ku = 404,
        Ku_Each = 405,
        Pu = 406,
        Pu_Each = 407,
        LEDState = 408,
        LEDState_Each = 409,
        AsservMode = 410,
        AsservMode_Each = 411,
        BatteryLevel = 412,
        BatteryLevel_Each = 413,
        X = 414,
        Y = 415,
        Theta = 416,
        XYTheta = 417,
        IMUData = 418,
        XYTheta_Speed = 419,                //0x01A3
        MotorsVitesses = 420,              //Ox01A4
        MotorsPositions = 421,          //0x01A5
        MotorsSpeedConsignes = 422,     //0x01A6
        TrajectoryData = 423,
        SetRobotPosition = 424,
        AddDeplacement = 425,
        SetAccelSpeed = 426,
        SetSpeedConsigne = 427,
        AsservissementData = 428,       //0x01AC
        SetTrajectoryState = 429,
        SetRobotVariable = 430,
        OdometryData = 431,             //0x01AF
        GetParameter = 432,
        SetParameter = 433,
        SetMotorSpeedConsigne = 434,
        EnableDisableMotors = 435,      //0x01B3
        EnableDisableTir = 436,         //0x01B4
        MotorCurrents= 437,             //0x01B5
        EnableAsservissement=438,       //0x01B6
        EnableMotorCurrent=439,         //0x01B7
        EnableEncoderRawData=440,       //0x01B8
        EnablePositionData=441,         //0x01B9
        EnableMotorSpeedConsigne=442,   //0x01BA
        GetCamera = 443,
        TirCommand = 444,
        MoveTirUp = 445,
        MoveTirDown = 446,



        // Setters de variables d'informations sur les robots

        SetID = 600,
        SetName = 601,
        SetColor = 602,
        SetControllerControlMode = 603,


        // Getters et Askers de variables d'informations sur les robots

        ID = 800,
        Name = 801,
        Color = 802,
        ControllerControlMode = 803,

        // Commandes haut niveau

        GoToXYTheta = 1000,

        ErrorTextMessage=-4370,

#pragma warning restore CS1591
        #endregion
        #region Commandes de la RoboCup

        /// <summary>Arrêt de jeu</summary>
        STOP = 'S',
        /// <summary>Prise ou reprise de jeu</summary>
        START = 's',
        /// <summary>Envoyé pour signaler une connection établie</summary>
        WELCOME = 'W',
        /// <summary>Commande inconnue.</summary> TODO
        WORLD_STATE = 'w',
        /// <summary>Remise à zéro du match</summary>
        RESET = 'Z',
        /// <summary>Reserved for RefBox debugging</summary>
        TESTMODE_ON = 'U',
        /// <summary>Reserved for RefBox debugging</summary>
        TESTMODE_OFF = 'u',
        /// <summary>Carton jaune Magenta</summary>
        YELLOW_CARD_MAGENTA = 'y',
        /// <summary>Carton jaune Cyan</summary>
        YELLOW_CARD_CYAN = 'Y',
        /// <summary>Carton rouge Magenta</summary>
        RED_CARD_MAGENTA = 'r',
        /// <summary>Carton rouge Cyan</summary>
        RED_CARD_CYAN = 'R',
        /// <summary>Commande inconnue.</summary> TODO
        DOUBLE_YELLOW_IN_MAGENTA = 'j',
        /// <summary>Commande inconnue.</summary> TODO
        DOUBLE_YELLOW_IN_CYAN = 'J',
        /// <summary>Début de la première mi-temps</summary>
        FIRST_HALF = '1',
        /// <summary>Début de la seconde mi-temps</summary>
        SECOND_HALF = '2',
        /// <summary>Début de la première mi-temps du temps additionnel</summary>
        FIRST_HALF_OVERTIME = '3',
        /// <summary>Début de la seconde mi-temps du temps additionnel</summary>
        SECOND_HALF_OVERTIME = '4',
        /// <summary>Fin de la première mi-temps (normal ou additionnel)</summary>
        HALF_TIME = 'h',
        /// <summary>Fin de la seconde mi-temps (normal ou additionnel)</summary>
        END_GAME = 'e',
        /// <summary>Commande inconnue.</summary> TODO
        GAMEOVER = 'z',
        /// <summary>Commande inconnue.</summary> TODO
        PARKING = 'L',
        /// <summary>But+ Magenta</summary>
        GOAL_MAGENTA = 'a',
        /// <summary>But+ Cyan</summary>
        GOAL_CYAN = 'A',
        /// <summary>But- Magenta</summary>
        SUBGOAL_MAGENTA = 'd',
        /// <summary>But- Cyan</summary>
        SUBGOAL_CYAN = 'D',
        /// <summary>Coup d'envoi Magenta</summary>
        KICKOFF_MAGENTA = 'k',
        /// <summary>Coup d'envoi Cyan</summary>
        KICKOFF_CYAN = 'K',
        /// <summary>Coup franc Magenta</summary>
        FREEKICK_MAGENTA = 'f',
        /// <summary>Coup franc Cyan</summary>
        FREEKICK_CYAN = 'F',
        /// <summary>Coup franc depuis le goal Magenta</summary>
        GOALKICK_MAGENTA = 'g',
        /// <summary>Coup franc depuis le goal Cyan</summary>
        GOALKICK_CYAN = 'G',
        /// <summary>Touche Magenta</summary>
        THROWIN_MAGENTA = 't',
        /// <summary>Touche Cyan</summary>
        THROWIN_CYAN = 'T',
        /// <summary>Corner Magenta</summary>
        CORNER_MAGENTA = 'c',
        /// <summary>Corner Cyan</summary>
        CORNER_CYAN = 'C',
        /// <summary>Penalty Magenta</summary>
        PENALTY_MAGENTA = 'p',
        /// <summary>Penalty Cyan</summary>
        PENALTY_CYAN = 'P',
        /// <summary>Balle lachée</summary>
        DROPPED_BALL = 'N',
        /// <summary>Robot parti en réparation Magenta</summary>
        REPAIR_OUT_MAGENTA = 'o',
        /// <summary>Robot parti en réparation Cyan</summary>
        REPAIR_OUT_CYAN = 'O',
        /// <summary>Commande inconnue.</summary>
        REPAIR_IN_MAGENTA = 'i',
        /// <summary>Commande inconnue.</summary>
        REPAIR_IN_CYAN = 'I'

        #endregion
    }

    public enum MotorControlName
    {
#pragma warning disable CS1591
        MotorLeft,
        MotorRear,
        MotorRight,
        Motor4,
        Motor5,
        Motor6,
        None
#pragma warning restore CS1591
    }
}
