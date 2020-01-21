using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BevGrafVizsgaBezier
{
    public partial class Form1 : Form
    {
        Graphics g;
        Brush brush;

        PointF p0, p1;
        List<PointF> P = new List<PointF>();
        Pen penBlack = Pens.Black;
        Pen penRed = Pens.Red;
        Pen pCurve = new Pen(Color.Blue, 3.0f);
        PointF pont;

        int found1 = -1;

        float m;
        bool found2 = true;
        int hibahatar = 4;
        int korszam = 0;

        public Form1()
        {
            InitializeComponent();
            brush = new SolidBrush(Color.Green);
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            //Meglévő pontok összekötése
            for (int i = 0; i < P.Count - 1; i++)
                g.DrawLine(penBlack, P[i], P[i + 1]);

            //Vizsgálom, hogy a következő pont amit le akarok tenni az egyenesre kell-e kerüljön
            if (P.Count == 4 + korszam * 3)
            {
                for (int i = 0; i < P.Count / 3; i++) //Kirajzolja a BezierGörbét az első 4 majd onnan minden további 4-4 pontra
                {
                    int k = i * 3;
                    DrawBezier(P[P.Count - 4 - k], P[P.Count - 3 - k], P[P.Count - 2 - k], P[P.Count - 1 - k]);
                }

                //generálni a piros vonalat az utolsó 2 pont alapján a lap széléig
                PointF C = kepszelehezpont(P[P.Count - 1], P[P.Count - 2]);
                g.DrawLine(penRed, P[P.Count - 1], C);
            }

            //Pontok kirajzlása 
            for (int i = 0; i < P.Count; i++)
            {
                g.FillEllipse(brush, P[i].X - hibahatar, P[i].Y - hibahatar, 2 * hibahatar, 2 * hibahatar);
                g.DrawEllipse(penBlack, P[i].X - hibahatar, P[i].Y - hibahatar, 2 * hibahatar, 2 * hibahatar);
            }
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < P.Count; i++) //Ha az egérrel egy pontra kattintottam
            {
                if (Math.Abs(P[i].X - e.X) <= hibahatar && Math.Abs(P[i].Y - e.Y) <= hibahatar)
                    found1 = i;
            }

            if (found1 == -1) //ha nem egy már meglévő pontra kattintottam
            {
                if (P.Count == 4 + korszam * 3) //Ha a most létrehozni kívánt pont korlátozás alá esik (az előző 2 egyenesére kéne kerüljön)
                {
                    found2 = false;
                    pont = e.Location;
                    rajtavan(P[P.Count - 2], P[P.Count - 1], pont); //Vizsgálat, hogy az előző 2 pont által rajzolt piros egyenesre tettem-e a pontot
                }
                if (found2) /*defaultba lefut és létrehoz 1 új pontot, azonban ha ez a pont a korlátozás alá eső akkor csak akkor lehet a found2 igaz
                                ha a rajtavan fgv lefutott és a vonalon hozom létra az új pontot*/
                {
                    P.Add(e.Location);
                    found1 = P.Count - 1;
                    Canvas.Invalidate();
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (found1 != -1)
            {
                /*vizsgálom, hogy az az elem indexe amire kattintottam az ha 2-őt hozzáadok 3 többszöröse-e. Hisz a korlátozottan mozgatható elemek
                    indexe 4,7,10 -> tehát 2-t hozzáadva 6,9,12-t kapok
                    Ha ezek egyikéről van szó akkor az egér helyzete alapján az egyenes egyenletével kiszámolom, hogy csak a vonalon lehessen
                    mozgatni.*/
                if (found1 != 0 && (found1 + 2) % 3 == 0)
                {
                    P[found1] = SzamolY(P[found1], P[found1 - 1], e.Location);
                }
                else if (found1 != 0 && found1 % 3 == 0 && found1 + 1 < P.Count) // az egyenest alkotó 2. pontról van szó
                {
                    P[found1] = e.Location;
                    P[found1 + 1] = SzamolY(P[found1 - 1], e.Location, P[found1 + 1]);
                }
                else if (found1 != 0 && (found1 + 1) % 3 == 0 && found1 + 2 < P.Count) //az egyenest alkotó 1. pont
                {
                    P[found1] = e.Location;
                    P[found1 + 2] = SzamolY(e.Location, P[found1 + 1], P[found1 + 2]);
                }
                else //ha nem arról van szó akkor pedig korlátozás nélkül áthelyezhető
                {
                    P[found1] = e.Location;
                }
                Canvas.Invalidate();
            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            found1 = -1;
        }

        private void DrawBezier(PointF A, PointF B, PointF C, PointF D)
        {
            List<PointF> list = new List<PointF>();
            list.Add(A);
            list.Add(B);
            list.Add(C);
            list.Add(D);

            double t = 0.0;
            double h = 1.0 / 500.0;
            p0 = new PointF(0f, 0f);
            int n = list.Count - 1;
            double b;
            for (int i = 0; i < list.Count; i++)
            {
                b = Bez(n, i, t);
                p0.X += (float)(b * list[i].X);
                p0.Y += (float)(b * list[i].Y);
            }
            while (t < 1.0)
            {
                t += h;
                p1 = new PointF(0f, 0f);
                for (int i = 0; i < list.Count; i++)
                {
                    b = Bez(n, i, t);
                    p1.X += (float)(b * list[i].X);
                    p1.Y += (float)(b * list[i].Y);
                }
                g.DrawLine(pCurve, p0, p1);
                p0 = p1;
            }
        }

        private double Bez(int n, int i, double t)
        {
            return Binom(n, i) * Math.Pow(1 - t, n - i) * Math.Pow(t, i);
        }

        private int Binom(int n, int k)
        {
            if (n == 0) return 0;
            else if (k == 0 || k == n) return 1;
            else return Binom(n - 1, k - 1) + Binom(n - 1, k);
        }

        private PointF kepszelehezpont(PointF p1, PointF p2) //pontot tesz a képernyő szélére
        {
            PointF uj = new PointF();
            m = (p2.Y - p1.Y) / (p2.X - p1.X);
            if (p1.X < p2.X)
                uj.X = 0;
            else
                uj.X = Canvas.Width;
            uj.Y = m * (uj.X - p1.X) + p1.Y;

            return uj;
        }

        private void rajtavan(PointF uelotti, PointF utolso, PointF pont) /*Vizsgálja, hogy megadott 2 pont alapjána 3.
                                                                            az általuk meghatározott egyenesre esik-e.*/
        {
            float temp = Math.Abs(m * (pont.X - utolso.X) + utolso.Y);
            if (uelotti.X <= utolso.X && utolso.X <= pont.X)
            {
                if (temp - hibahatar <= Math.Abs(pont.Y) && Math.Abs(pont.Y) <= temp + hibahatar)
                {
                    korszam++;
                    found2 = true;
                }

            }
            else if (uelotti.X >= utolso.X && utolso.X >= pont.X)
            {
                if (temp - hibahatar <= Math.Abs(pont.Y) && Math.Abs(pont.Y) <= temp + hibahatar)
                {
                    korszam++;
                    found2 = true;
                }
            }
        }
        private PointF SzamolY(PointF p1, PointF p2, PointF p3) /*2 pont alapján számol egyenest és visszaad egy új pontot
                                                                a paraméterül bekapott 3. pont x értéke és az első 2 pont által meghatározott egyenes
                                                                egyenlete alapján. Y-t számolja ki*/
        {
            PointF uj = new PointF();
            float m2 = (p2.Y - p1.Y) / (p2.X - p1.X);
            uj.X = p3.X;
            uj.Y = m2 * (uj.X - p1.X) + p1.Y;

            return uj;
        }
    }
}
