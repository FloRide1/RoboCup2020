%Calcul de la frÃ©quence du dsPIC33EP512MU814 sur Oscillateur Fast RC en
%mode FRCPLL (Internal Fast RC with PLL)
%Contraintes : F_IN/(PLL_PRE+2) entre 0.8 MHz et 8 MHz
%Contraintes : F_VCO = F_IN*(PLL_DIV+2)/(PLL_PRE+2) entre 120 MHz et 340 MHz
%Contraintes : F_OSC entre 15 MHz et 140 MHz Ã  85Â°

%Cas avec oscillateur principal
Fin = 32; %7.37;
PLLPOST = 0;
PLLPRE = 4;
PLLDIV = 43;

%Cas avec oscillateur FRC tuning à 8MHz avec TUN=23
Fin = 8;
PLLPOST = 0;
PLLPRE = 1;
PLLDIV = 88;

FPLLI = Fin/(PLLPRE+2);             %Contraintes : F_IN/(PLL_PRE+2) entre 0.8 MHz et 8 MHz
FVCO = FPLLI*(PLLDIV+2);            %Contraintes : F_VCO = F_IN*(PLL_DIV+2)/(PLL_PRE+2) entre 120 MHz et 340 MHz
FPLLOUT = FVCO/(2*(PLLPOST+1));     %Contraintes : F_OSC entre 15 MHz et 140 MHz à 85°

%Oscillateur auxiliaire pour l'USB PLL en mode PRI avec quartz à 32MHz
% Fin = 32;
% APLLPRECoeff = 6;  %Coeff signifie qu'il ya une table de lookup entre la valeur et le registre
% APLLDIVCoeff = 18;
% APLLPOSTCoeff = 2;

%Oscillateur auxiliaire pour l'USB PLL en mode FRC 8MHz corrigé TUN
Fin = 8;
APLLPRECoeff = 2;  %Coeff signifie qu'il ya une table de lookup entre la valeur et le registre
APLLDIVCoeff = 24;
APLLPOSTCoeff = 2;

if(APLLPRECoeff<=6)
    APLLPRE = APLLPRECoeff-1;
elseif(APLLPRECoeff==10)
    APLLPRE = 6;
elseif(APLLPRE == 12)
    APLLPRE=7;
end

if(APLLDIVCoeff<=21)
    APLLDIV = APLLDIVCoeff-15;
elseif(APLLDIVCoeff==24)
    APLLDIV = 7;
end

switch(APLLPOSTCoeff)
    case 1
        APLLPOST=7;
    case 2
        APLLPOST=6;
    case 4
        APLLPOST=5;
    case 8
        APLLPOST=4;
    case 16
        APLLPOST=3;
    case 32
        APLLPOST=2;
    case 64
        APLLPOST=1;
    case 256
        APLLPOST=0;
end

AFPLLI = Fin/(APLLPRECoeff);           %Contraintes : F_IN/(PLL_PRE+2) entre 3 MHz et 5.5 MHz
AFVCO = AFPLLI*(APLLDIVCoeff);        %Contraintes : F_VCO = F_IN*(PLL_DIV+2)/(PLL_PRE+2) entre 60 MHz et 120 MHz
AFPLLOUT = AFVCO/(APLLPOSTCoeff);     %Contraintes : F_OSC à 48Mhz obligatoire