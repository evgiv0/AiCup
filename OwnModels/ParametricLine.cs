using System.Collections.Generic;
using System.Drawing;
using AiCup2019.Model;

namespace AiCup2019.OwnModels
{
    public class ParametricLine
    {
        PointF p1;
        PointF p2;

        public ParametricLine(PointF p1, PointF p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        public PointF Fraction(float frac)
        {
            return new PointF(p1.X + frac * (p2.X - p1.X),
                p1.Y + frac * (p2.Y - p1.Y));
        }

        public IEnumerable<PointF> GetLinePoints(int cnt)
        {
            var deltaX = p1.X - p2.X;
            var deltaY = p1.Y - p2.Y;

            var lastPoint = p2;
            var curPoint = new PointF();

            var result = new PointF[cnt];
            for (int i = 0; i < cnt; i++)
            {
                curPoint = new PointF(lastPoint.X + deltaX, lastPoint.Y + deltaY);
                lastPoint = curPoint;
                result[i] = curPoint;
            }

            return result;
        }

        public bool СontainsPoint(Vec2Double currentBulletPosition)
        {
            throw new System.NotImplementedException();
        }
    }
}