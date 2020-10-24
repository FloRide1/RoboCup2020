using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Utilities
{
    public class AsservissementPID
    {
        double Kp = 0, Ki = 0, Kd = 0;
        double error, errorT_1;
        double IntegraleErreur = 0;
        double ProportionalLimit = 0;
        double IntegralLimit = 0;
        double DerivationLimit = 0;
        double SampleFreq;

        public AsservissementPID(double fEch, double kp, double ki, double kd, double proportionalLimit, double integralLimit, double derivationLimit)
        {
            Kp = kp;
            Ki = ki;
            Kd = kd;

            ProportionalLimit = proportionalLimit;
            IntegralLimit = integralLimit;
            DerivationLimit = derivationLimit;

            IntegraleErreur = 0;
            SampleFreq = fEch;
        }

        public void ResetPID(double error)
        {
            IntegraleErreur = 0;
            errorT_1 = error;
        }

        public double CalculatePIDoutput(double error)
        {
            //Le principe de calcul est le suivant :
            //On veut borner les corrections sur chaque terme à une valeur donnée, par exemple ProportionalLimit pour la contribution de P à la correction
            //Sachant que correctionP = Kp*erreur, il faut donc borner au préalable erreur à ProportionalLimit / Kp

            double erreurBornee = Toolbox.LimitToInterval(error, -ProportionalLimit / Kp, ProportionalLimit / Kp);
            double correctionP = Kp * erreurBornee;


            IntegraleErreur += error / SampleFreq;
            IntegraleErreur = Toolbox.LimitToInterval(IntegraleErreur, -IntegralLimit / Ki, IntegralLimit / Ki); //On touche à Integrale directement car on ne veut pas laisser l'intégrale grandir à l'infini
            double correctionI = Ki * IntegraleErreur;

            double derivee = (error - errorT_1) * SampleFreq;
            double deriveeBornee = Toolbox.LimitToInterval(derivee, -DerivationLimit / Kd, DerivationLimit / Kd);
            errorT_1 = error;
            double correctionD = deriveeBornee * Kd;

            return correctionP + correctionI + correctionD;
        }
    }
}
