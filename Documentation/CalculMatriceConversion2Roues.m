
%Angles des moteurs Eurobot 2 roues
alpha1 = -pi/2;
alpha2 = pi/2;
R = 0.120; %Si on int�gre directement le rayon
pointToMm = (0.059*pi)/(19.2*8192);

%Les vitesses angulaire des moteurs sont vues de l'int�rieur du moteur
%Une vitesse angulaire positive conduit � un d�placement n�gatif
VXTheta = [
-sin(alpha1) -sin(alpha2); 
R R];

V12 = pinv(VXTheta);
fprintf('OnOdometryPointToMeter(%d);\n', pointToMm);
fprintf('On2WheelsAngleSetup(%d, %d);\n',alpha1, alpha2); 
fprintf('On2WheelsToPolarSetup(%d, %d,\n', V12(1,1), V12(2,1));
fprintf('\t\t\t\t\t%d, %d);\n', V12(1,2), V12(2,2));
%fprintf('robotState.thetaVitesseFromOdometry = %d/Rayon*v1 +%d/Rayon*v2 +%d/Rayon*v3 +%d/Rayon*v4;\n', V1234(1,3), V1234(2,3), V1234(3,3), V1234(4,3));