using AiCup2019;
using AiCup2019.Model;
using AiCup2019.OwnModels;
using System;
using System.Drawing;
using System.Linq;

public class MyStrategy
{
    static double DistanceSqr(Vec2Double a, Vec2Double b)
    {
        return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    }
    public UnitAction GetAction(Unit unit, Game game, Debug debug)
    {
        Unit? nearestEnemy = null;
        foreach (var other in game.Units)
        {
            if (other.PlayerId != unit.PlayerId)
            {
                if (!nearestEnemy.HasValue || DistanceSqr(unit.Position, other.Position) < DistanceSqr(unit.Position, nearestEnemy.Value.Position))
                {
                    nearestEnemy = other;
                }
            }
        }

        var homePosition = unit.Position;
        LootBox? nearestWeapon = null;
        LootBox? nearestNotBazuka = null;
        LootBox? bestWeapon = null;
        LootBox? nearestHealth = null;
        LootBox? nearestMine = null;
        foreach (var lootBox in game.LootBoxes)
        {
            switch (lootBox.Item)
            {
                case Item.Weapon item:
                    {
                        if (!nearestWeapon.HasValue || DistanceSqr(unit.Position, lootBox.Position) < DistanceSqr(unit.Position, nearestWeapon.Value.Position))
                        {
                            nearestWeapon = lootBox;
                            if (item.WeaponType != WeaponType.RocketLauncher)
                                nearestNotBazuka = lootBox;
                        }

                        var anotherWeapon = item;
                        var myWeapon = unit.Weapon;

                        if (myWeapon.HasValue && myWeapon.Value.Typ < anotherWeapon.WeaponType)
                        {
                            bestWeapon = lootBox;
                        }

                        break;
                    }
                case Item.HealthPack _:
                    {
                        if (!nearestHealth.HasValue || DistanceSqr(unit.Position, lootBox.Position) < DistanceSqr(unit.Position, nearestHealth.Value.Position))
                        {
                            nearestHealth = lootBox;
                        }

                        break;
                    }
                case Item.Mine _:
                    {
                        if (!nearestMine.HasValue || DistanceSqr(unit.Position, lootBox.Position) < DistanceSqr(unit.Position, nearestMine.Value.Position))
                        {
                            nearestMine = lootBox;
                        }

                        break;
                    }
            }
        }

        var targetPos = GetTarget(new CurrentInfo
        {
            Me = unit,
            Enemy = nearestEnemy,
            NearestHealth = nearestHealth,
            NearestMine = nearestMine,
            NearestWeapon = nearestWeapon,
            NearestNotBazuka = nearestNotBazuka,
            BestWeapon = bestWeapon,
            Game = game
        });


        debug.Draw(new CustomData.Log("Target pos: " + targetPos));

        Vec2Double aim = new Vec2Double(0, 0);
        if (nearestEnemy.HasValue)
        {
            //if(!nearestEnemy.Value.OnGround && nearestEnemy.Value.)
            aim = new Vec2Double(nearestEnemy.Value.Position.X - unit.Position.X, nearestEnemy.Value.Position.Y - unit.Position.Y);
            debug.Draw(new CustomData.Line(new Vec2Float((float)unit.Position.X, (float)unit.Position.Y), new Vec2Float((float)aim.X, (float)aim.Y), 0.1f, new ColorFloat(255, 0, 0, 1)));

        }

        Bullet? nearestBullet = null;
        foreach (var bullet in game.Bullets.Where(x => x.PlayerId == unit.PlayerId))
        {

            if (!nearestBullet.HasValue || DistanceSqr(unit.Position, bullet.Position) < DistanceSqr(unit.Position, bullet.Position))
            {
                nearestBullet = bullet;
            }
        }


        if (nearestBullet.HasValue)
        {
            if (unit.Position.X > nearestBullet.Value.Position.X
                && unit.Weapon.HasValue
                && unit.Weapon.Value.Parameters.Explosion.HasValue
                && unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius < nearestBullet.Value.Position.X)
            {
                targetPos = homePosition;
            }
            else if (unit.Position.X < nearestBullet.Value.Position.X
                && unit.Weapon.HasValue
                && unit.Weapon.Value.Parameters.Explosion.HasValue
                && unit.Position.X + unit.Weapon.Value.Parameters.Explosion.Value.Radius > nearestBullet.Value.Position.X)
            {
                targetPos = homePosition;
            }

        }

        bool jump = targetPos.Y > unit.Position.Y;
        if (targetPos.X > unit.Position.X && game.Level.Tiles[(int)(unit.Position.X + 1)][(int)(unit.Position.Y)] == Tile.Wall)
        {
            jump = true;
        }


        if (targetPos.X < unit.Position.X && game.Level.Tiles[(int)(unit.Position.X - 1)][(int)(unit.Position.Y)] == Tile.Wall)
        {
            jump = true;
        }

        if (targetPos.Y < unit.Position.Y && game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y - 1)] == Tile.Ladder &&
                                               game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y)] != Tile.Ladder)
        {
            jump = false;
        }

        bool shoot = true;

        //shoot = IsAnyWalls(unit.Position, aim);
        var isBazukaBlya = unit.Weapon.HasValue && unit.Weapon.Value.Typ == WeaponType.RocketLauncher &&
                           unit.Weapon.Value.Parameters.Explosion.HasValue;
        if (isBazukaBlya)
        {
            shoot = CanIShoot(unit, aim, game, debug, nearestEnemy.Value);

        }


        double velocity = targetPos.X - unit.Position.X;
        if (nearestBullet.HasValue)
            velocity = targetPos.X - unit.Position.X;
        else if (unit.Position.X > targetPos.X && velocity > -game.Properties.UnitMaxHorizontalSpeed)
            velocity = -game.Properties.UnitMaxHorizontalSpeed;
        else if (unit.Position.X > nearestEnemy?.Position.X && isBazukaBlya &&
                 unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2 < nearestEnemy?.Position.X)
        {
            jump = true;
            velocity = game.Properties.UnitMaxHorizontalSpeed;
        }
        else if (unit.Position.X < targetPos.X && velocity < game.Properties.UnitMaxHorizontalSpeed)
            velocity = game.Properties.UnitMaxHorizontalSpeed;
        else if (unit.Position.X < nearestEnemy?.Position.X && isBazukaBlya &&
                 unit.Position.X + unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2 > nearestEnemy?.Position.X)
        {
            jump = true;
            velocity = -game.Properties.UnitMaxHorizontalSpeed;
        }





        //if (unit.Position.X > targetPos.X && unit.Position.X - )

        //не стрелять если в ближайших клетках есть стена
        //
        //научить стрелять из базуки
        //если идешь назад, кидать мины?
        //не приближаться близко, если у тебя базука или у врага базука

        UnitAction action = new UnitAction
        {
            Velocity = velocity,
            Jump = jump,
            JumpDown = !jump,
            Aim = aim,
            Shoot = shoot,
            SwapWeapon = bestWeapon.HasValue,
            PlantMine = false
        };
        return action;
    }

    private bool CanIShoot(Unit unit, Vec2Double aim, Game game, Debug debug, Unit enemy)
    {
        var line = new ParametricLine(new PointF((float)unit.Position.X, (float)unit.Position.Y), new PointF((float)enemy.Position.X, (float)enemy.Position.Y));
        debug.Draw(new CustomData.Line(new Vec2Float((float)unit.Position.X, (float)unit.Position.Y), new Vec2Float((float)aim.X, (float)aim.Y), 0.1f, new ColorFloat(255, 0, 0, 1)));
        var fraction = Math.Abs((int)unit.Position.X) + Math.Abs((int)enemy.Position.X);
        var points = Enumerable.Range(0, fraction)
            .Select(p => line.Fraction((float)p / fraction));

        if (unit.Position.Y > enemy.Position.Y)
        {
            for (int i = 0; i < unit.Weapon.Value.Parameters.Explosion.Value.Radius; i++)
            {
                if (game.Level.Tiles[(int)unit.Position.X][(int)unit.Position.Y - i] == Tile.Wall)
                    return false;
            }
        }

        if (Math.Abs(unit.Position.X - enemy.Position.X) < unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2
            && unit.Health > unit.Weapon.Value.Parameters.Explosion.Value.Damage + unit.Weapon.Value.Parameters.FireRate)
            return true;

        
        if (unit.Position.X > enemy.Position.X)
        {
            foreach (var pointF in points.Where(x => x.X > unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2))
            {
                //debug.Draw
                if (game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Wall
                    || game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Platform
                    || game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.JumpPad)
                    return false;
            }
        }
        else if (unit.Position.X < enemy.Position.X)
        {
            foreach (var pointF in points.Where(x => x.X < unit.Position.X + unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2))
            {
                if (game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Wall)
                    return false;
            }
        }

        return true;

    }

    //private bool IsAnyWalls(Vec2Double unitPosition, Vec2Double aim)
    //{
    //    for
    //}

    private Vec2Double GetTarget(CurrentInfo currentInfo)
    {
        if (!currentInfo.Me.Weapon.HasValue && currentInfo.NearestWeapon.HasValue)
        {
            return currentInfo.NearestWeapon.Value.Position;
        }

        if (currentInfo.Me.Health <= currentInfo.Game.Properties.UnitMaxHealth * 0.9 && currentInfo.NearestHealth.HasValue)
        {
            return currentInfo.NearestHealth.Value.Position;
        }

        if (currentInfo.BestWeapon.HasValue && currentInfo.Me.Weapon.HasValue && currentInfo.Me.Weapon.Value.Typ != WeaponType.RocketLauncher)
        {
            return currentInfo.BestWeapon.Value.Position;
        }
        if (currentInfo.Enemy.HasValue)
        {
            return currentInfo.Enemy.Value.Position;
        }

        return currentInfo.Me.Position;
    }
}