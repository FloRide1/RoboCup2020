%Calcul de la fréquence du dsPIC33EP512MU814 sur Oscillateur Fast RC en
%mode FRCPLL (Internal Fast RC with PLL)
%Contraintes : F_IN/(PLL_PRE+2) entre 0.8 MHz et 8 MHz
%Contraintes : F_VCO = F_IN*(PLL_DIV+2)/(PLL_PRE+2) entre 120 MHz et 340 MHz
%Contraintes : F_OSC entre 15 MHz et 140 MHz à 85°

Fin = 32; %7.37;
PLLPOST = 0;
PLLPRE = 4;
PLLDIV = 43;

FPLLI = Fin/(PLLPRE+2);
FVCO = FPLLI*(PLLDIV+2);
FPLLOUT = FVCO/(2*(PLLPOST+1));

