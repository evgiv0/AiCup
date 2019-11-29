using System.Drawing;

namespace AiCup2019.OwnModels
{
    public class ParametricLine{
        PointF p1;
        PointF p2;
   
        public ParametricLine(PointF p1, PointF p2) {
            this.p1 = p1;
            this.p2 = p2;
        }
   
        public PointF Fraction(float frac) {
            return new PointF( p1.X + frac*(p2.X-p1.X),
                p1.Y + frac*(p2.Y-p1.Y));
        }
    }
}