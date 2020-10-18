%Calcul de la fréquence du dsPIC33EP512MU814 sur Oscillateur Fast RC en
%mode FRCPLL (Internal Fast RC with PLL)

Fin = 7.37;
PLLPOST = 0;
PLLPRE = 0;
PLLDIV = 63;

FPLLI = Fin/(PLLPRE+2);
FVCO = FPLLI*(PLLDIV+2);
FPLLOUT = FVCO/(2*(PLLPOST+1));

