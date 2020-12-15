
%Angles des moteurs RoboCup
alpha1 = 2*pi/5;
alpha2 = 4*pi/5;
alpha3 = 6*pi/5;
alpha4 = 8*pi/5;
R = 0.185; %Si on intègre directement le rayon
pointToMm = (0.127*pi*1.4)/(16*8192);

%Angles des moteurs Eurobot
% alpha1 = deg2rad(59);
% alpha2 = deg2rad(144);
% alpha3 = deg2rad(216);
% alpha4 = deg2rad(301);
% R = 0.145; %Si on intègre directement le rayon
% pointToMm = (0.06*pi)/(19*8192);

%Les vitesses angulaire des moteurs sont vues de l'intérieur du moteur
%Une vitesse angulaire positive conduit à un déplacement négatif
VXYTheta = [
-sin(alpha1) -sin(alpha2) -sin(alpha3) -sin(alpha4);
cos(alpha1) cos(alpha2) cos(alpha3) cos(alpha4);
R R R R];

V1234 = pinv(VXYTheta);
fprintf('OnOdometryPointToMeter(%d);\n', pointToMm);
fprintf('On4WheelsAngleSet(%d, %d, %d, %d);\n',alpha1, alpha2, alpha3, alpha4); 
fprintf('On4WheelsToPolarSet(%d, %d, %d, %d,\n', V1234(1,1), V1234(2,1), V1234(3,1), V1234(4,1));
fprintf('\t\t\t\t\t%d, %d, %d, %d,\n', V1234(1,2), V1234(2,2), V1234(3,2), V1234(4,2));
fprintf('\t\t\t\t\t%d, %d, %d, %d); \n', V1234(1,3), V1234(2,3), V1234(3,3), V1234(4,3));
%fprintf('robotState.thetaVitesseFromOdometry = %d/Rayon*v1 +%d/Rayon*v2 +%d/Rayon*v3 +%d/Rayon*v4;\n', V1234(1,3), V1234(2,3), V1234(3,3), V1234(4,3));