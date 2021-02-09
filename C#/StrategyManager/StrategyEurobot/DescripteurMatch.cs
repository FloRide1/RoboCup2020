using Constants;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace StrategyManagerEurobotNS
{
    class MatchDescriptor
    {
        List<Gobelet> listGobelets = new List<Gobelet>();
        List<EmplacementDepose> listEmplacements = new List<EmplacementDepose>();

        public MatchDescriptor()
        {
            FillGobeletList();
            FillEmplacementList();
        }

        private void FillGobeletList()
        {
            listGobelets.Add(new Gobelet(1, -1.200, 0.200, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(2, -1.200, -0.600, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(3, -1.050, 0.120, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(4, -1.050, -0.490, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(5, -0.800, -0.900, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(6, -0.550, -0.600, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(7, -0.400, -0.200, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(8, -0.230, 0.200, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(9, 0.230, 0.200, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(10, 0.400, -0.200, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(11, 0.550, -0.600, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(12, 0.800, -0.900, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(13, 1.050, 0.120, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(14, 1.050, -0.490, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(15, 1.200, 0.200, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(16, 1.2, 0.6, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(17, -0.495, 0.955, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(18, -0.435, 0.65, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(19, -0.165, 0.65, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(20, -0.105, 0.955, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(21, 0.105, 0.955, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(22, 0.165, 0.65, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(23, 0.435, 0.65, Color.Vert, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(24, 0.495, 0.955, Color.Rouge, TypeGobelet.Libre));
            listGobelets.Add(new Gobelet(25, -1.567, 750, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(26, -1.567, 0.675, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(27, -1.567, 0.6, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(28, -1.567, 0.525, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(29, -1.567, 0.45, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(30, -0.8, -1.067, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(31, -0.725, -1.067, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(32, -0.65, -1.067, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(33, -0.575, -1.067, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(34, -0.5, -1.067, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(35, 0.5, -1.067, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(36, 0.575, -1.067, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(37, 0.65, -1.067, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(38, 0.725, -1.067, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(39, 0.8, -1.067, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(40, 1.567, 0.75, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(41, 1.567, 0.675, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(42, 1.567, 0.6, Color.Rouge, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(43, 1.567, 0.525, Color.Vert, TypeGobelet.Distributeur));
            listGobelets.Add(new Gobelet(44, 1.567, 0.450, Color.Rouge, TypeGobelet.Distributeur));
        }

        void FillEmplacementList()
        {

            //Emplacement Jaune coté Gauche
            EmplacementDepose emplacement = new EmplacementDepose(-1.1, -0.49, Color.Rouge, Equipe.Jaune);
            int num = 1;
            for (int j = 0; j < 4; j++)
                emplacement.positionsDeposeList.Add(num++, new CaseDepose(-1.1 - j * 0.08, -0.49));
            listEmplacements.Add(emplacement);
            
            emplacement = new EmplacementDepose(-1.1, -0.205, Color.Neutre, Equipe.Jaune);
            num = 1;
            for (int i = -1; i < 2; i++)
            {
                for (int j = 0; j < 4; j++)
                    emplacement.positionsDeposeList.Add(num++, new CaseDepose(-1.1 - j * 0.08, -0.205 + i * 0.15));
            }
            listEmplacements.Add(emplacement);

            emplacement = new EmplacementDepose(-1.1, 0.08, Color.Vert, Equipe.Jaune);
            num = 1;
            for (int j = 0; j < 4; j++)
                emplacement.positionsDeposeList.Add(num++, new CaseDepose(-1.1 - j * 0.08, 0.08));
            listEmplacements.Add(emplacement);

            //Emplacement Bleu coté Droit
            emplacement = new EmplacementDepose(1.1, -0.49, Color.Vert, Equipe.Bleue);
            num = 1;
            for (int j = 0; j < 4; j++)
                emplacement.positionsDeposeList.Add(num++, new CaseDepose(1.1 + j * 0.08, -0.49));
            listEmplacements.Add(emplacement);

            emplacement = new EmplacementDepose(1.1, -0.205, Color.Neutre, Equipe.Bleue);
            num = 1;
            for (int i = -1; i < 2; i++)
            {
                for (int j = 0; j < 4; j++)
                    emplacement.positionsDeposeList.Add(num++, new CaseDepose(1.1 + j * 0.08, -0.205 + i * 0.15));
            }
            listEmplacements.Add(emplacement);

            emplacement = new EmplacementDepose(1.1, 0.08, Color.Rouge, Equipe.Bleue);
            num = 1;
            for (int j = 0; j < 4; j++)
                emplacement.positionsDeposeList.Add(num++, new CaseDepose(1.1 + j * 0.08, 0.08));
            listEmplacements.Add(emplacement);

            //Emplacement Bleu en haut
            emplacement = new EmplacementDepose(-0.4, 0.7, Color.Rouge, Equipe.Bleue);
            num = 1;
            for (int j = 3; j >= 0; j--) //Attention au sens des indices pour ranger au fond en premier
            { 
                for (int i = 0; i < 2; i++)
                    emplacement.positionsDeposeList.Add(num++, new CaseDepose(-0.44+i*0.8, 0.7 + j * 0.08));
            }
            listEmplacements.Add(emplacement);

            emplacement = new EmplacementDepose(-0.2, 0.7, Color.Vert, Equipe.Bleue);
            num = 1;
            for (int j = 3; j >= 0; j--) //Attention au sens des indices pour ranger au fond en premier
            {
                for (int i = 0; i < 2; i++)
                    emplacement.positionsDeposeList.Add(num++, new CaseDepose(-0.24 + i * 0.8, 0.7 + j * 0.08));
            }
            listEmplacements.Add(emplacement);


            //Emplacement Jaune en haut
            emplacement = new EmplacementDepose(-0.4, 0.7, Color.Vert, Equipe.Jaune);
            num = 1;
            for (int j = 3; j >= 0; j--) //Attention au sens des indices pour ranger au fond en premier
            {
                for (int i = 0; i < 2; i++)
                    emplacement.positionsDeposeList.Add(num++, new CaseDepose(0.44 - i * 0.8, 0.7 + j * 0.08));
            }
            listEmplacements.Add(emplacement);

            emplacement = new EmplacementDepose(0.2, 0.7, Color.Rouge, Equipe.Jaune);
            num = 1;
            for (int j = 3; j >= 0; j--) //Attention au sens des indices pour ranger au fond en premier
            {
                for (int i = 0; i < 2; i++)
                    emplacement.positionsDeposeList.Add(num++, new CaseDepose(0.24 - i * 0.8, 0.7 + j * 0.08));
            }
            listEmplacements.Add(emplacement);

        }

    }

    public enum Color
    {
        Vert,
        Rouge,
        Neutre
    }

    public enum TypeGobelet
    {
        Libre,
        Distributeur
    }


    class Gobelet
    {
        public PointD Pos;
        public Color Color;
        public TypeGobelet Type;
        public bool isAvailable;
        public byte Id;

        public Gobelet(byte id, double x, double y, Color color, TypeGobelet type)
        {
            Id = id;
            Pos = new PointD(x, y);
            Color = color;
            Type = type;
            isAvailable = true;
        }
    }

    class EmplacementDepose
    {
        public Dictionary<int, CaseDepose> positionsDeposeList = new Dictionary<int, CaseDepose>();
        public PointD Pos;
        public Equipe Equipe;
        public Color Color;

        public EmplacementDepose(double x, double y, Color couleur, Equipe team)
        {
            Pos = new PointD(x, y);
            Color = couleur;
            Equipe = team;
            positionsDeposeList = new Dictionary<int, CaseDepose>();
        }
    }

    class CaseDepose
    {
        public PointD Pos;
        public bool isFull;

        public CaseDepose(double x, double y)
        {
            Pos = new PointD(x, y);
            isFull = false;
        }
    }
}
