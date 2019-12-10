using AiCup2019;
using AiCup2019.Model;
using AiCup2019.OwnModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using aicup2019;

public class MyStrategy
{
    static double DistanceSqr(Vec2Double a, Vec2Double b)
    {
        return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    }

    public bool? InJump { get; set; }
    public bool GetPlatfrom { get; set; }
    public List<BulletInfo> EnemyBullets = new List<BulletInfo>();

    public Vec2Double? CurrentTarget;
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
            if (CanITakeThisLootBox(unit.Position, lootBox.Position, nearestEnemy))
                switch (lootBox.Item)
                {
                    case Item.Weapon item:
                        {
                            if (!nearestWeapon.HasValue || DistanceSqr(unit.Position, lootBox.Position) < DistanceSqr(unit.Position, nearestWeapon.Value.Position))
                            {
                                nearestWeapon = lootBox;
                            }

                            var anotherWeapon = item;
                            var myWeapon = unit.Weapon;

                            if (myWeapon.HasValue && myWeapon.Value.Typ < anotherWeapon.WeaponType)
                            {
                                bestWeapon = lootBox;
                            }

                            if (item.WeaponType != WeaponType.RocketLauncher)
                            {
                                nearestNotBazuka = lootBox;
                            }

                            break;
                        }
                    case Item.HealthPack _:
                        {
                            if (!nearestHealth.HasValue
                                || DistanceSqr(unit.Position, lootBox.Position) < DistanceSqr(unit.Position, nearestHealth.Value.Position))
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

        if (nearestEnemy.HasValue && game.Bullets.Any(x => x.PlayerId == nearestEnemy.Value.PlayerId))
        {
            var getNotFilledBullet = EnemyBullets.FirstOrDefault(x => !x.SecondPoint.HasValue);
            var currentBullet = game.Bullets.First(x => x.PlayerId == nearestEnemy.Value.PlayerId);


            if (EnemyBullets.Count == 0 || EnemyBullets.All(x => !x.СontainsPoint(currentBullet.Position)))
            {
                if (getNotFilledBullet == null)
                {
                    EnemyBullets.Add(new BulletInfo
                    {
                        FirstPoint = currentBullet.Position
                    });
                }
                else if (!getNotFilledBullet.SecondPoint.HasValue)
                    getNotFilledBullet.SecondPoint = currentBullet.Position;
            }
        }

        var target = GetTarget(new CurrentInfo
        {
            Me = unit,
            Enemy = nearestEnemy,
            NearestHealth = nearestHealth,
            NearestMine = nearestMine,
            NearestWeapon = nearestWeapon,
            NearestNotBazuka = nearestNotBazuka,
            HomePosition = homePosition,
            BestWeapon = bestWeapon,
            Game = game
        }, debug);

        debug.Draw(new CustomData.Log("Target pos: " + target.Position));

        var shoot = CanIShoot(unit, game, debug, nearestEnemy);

        var targetPos = target.Position;
        //var jump = ComputeJump(target, unit, game, nearestEnemy);
        bool jump = targetPos.Y > unit.Position.Y;
        if (target.Purpose == Purpose.NeoMode)
            InJump = target.NeedJump;
        else
            InJump = null;

        if (targetPos.X > unit.Position.X && game.Level.Tiles[(int)(unit.Position.X + 1)][(int)(unit.Position.Y)] == Tile.Wall)
        {
            jump = true;
        }
        if (targetPos.X < unit.Position.X && game.Level.Tiles[(int)(unit.Position.X - 1)][(int)(unit.Position.Y)] == Tile.Wall)
        {
            jump = true;
        }


        UnitAction action = new UnitAction
        {
            Velocity = ComputeVelocity(target, unit, game),
            Jump = jump,
            JumpDown = !jump,
            Aim = GetAim(unit, nearestEnemy),
            Shoot = false,//CanIShoot(unit, game, debug, nearestEnemy),
            Reload = false,
            SwapWeapon = target.SwapWeapon,
            PlantMine = false
        };
        return action;
    }


    private static Vec2Double GetAim(Unit unit, Unit? nearestEnemy)
    {
        Vec2Double aim = new Vec2Double(0, 0);
        if (nearestEnemy.HasValue)
        {
            aim = new Vec2Double(nearestEnemy.Value.Position.X - unit.Position.X, nearestEnemy.Value.Position.Y - unit.Position.Y);
        }

        return aim;
    }

    //private Jump ComputeJump(Target target, Unit unit, Game game, Unit? nearestEnemy)
    //{
    //    var targetPos = target.Position;
    //    var jump = new Jump();
    //    jump.JumpUp = targetPos.Y > unit.Position.Y;
    //    jump.JumpDown = false;
    //    if (targetPos.X > unit.Position.X && game.Level.Tiles[(int)(unit.Position.X + 1)][(int)(unit.Position.Y)] == Tile.Wall)

    //        jump.JumpUp = true;

    //    else
    //        jump.JumpUp = false;


    //    else if (targetPos.X < unit.Position.X && game.Level.Tiles[(int)(unit.Position.X - 1)][(int)(unit.Position.Y)] == Tile.Wall)
    //    {
    //        if (InJump && game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y - 1)] == Tile.Platform)
    //        {
    //            InJump = false;
    //            jump.JumpUp = false;
    //        }
    //        else
    //        {
    //            InJump = true;
    //            jump.JumpUp = true;
    //        }
    //    }

    //    else if (targetPos.Y < unit.Position.Y && game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y - 1)] == Tile.Ladder &&
    //                                           game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y)] != Tile.Ladder)
    //    {
    //        jump.JumpDown = true;
    //        InJump = false;
    //    }

    //    else if ((int)unit.Position.Y == (int)nearestEnemy.Value.Position.Y && Math.Abs((int)unit.Position.X - (int)nearestEnemy.Value.Position.X) < 2)
    //    {
    //        jump.JumpUp = true;
    //    }

    //    return jump;
    //}

    private double ComputeVelocity(Target target, Unit unit, Game game)
    {
        var targetPos = target.Position;
        double velocity;
        if (unit.Position.X > targetPos.X)
            velocity = -game.Properties.UnitMaxHorizontalSpeed;
        else
            velocity = game.Properties.UnitMaxHorizontalSpeed;


        return velocity;
    }

    private bool CanITakeThisLootBox(Vec2Double unitPosition, Vec2Double targetPosition, Unit? nearestEnemy)
    {
        if (!nearestEnemy.HasValue)
            return true;

        if (targetPosition.Y > unitPosition.Y
            && (int)unitPosition.X == (int)nearestEnemy.Value.Position.X
            && (int)unitPosition.Y < (int)nearestEnemy.Value.Position.Y)
            return false;

        if (targetPosition.Y < unitPosition.Y
            && (int)unitPosition.X == (int)nearestEnemy.Value.Position.X
            && (int)unitPosition.X == (int)targetPosition.X
            && (int)unitPosition.Y > (int)nearestEnemy.Value.Position.Y)
            return false;

        //if(unitPosition.X > nearestEnemy.Value.Position.X
        //    && targetPosition.X < unitPosition.X
        //    && targetPosition.X < nearestEnemy.Value.Position.X 
        //    && 
        //    ) 
        //if(targetPosition.)


        return true;
    }

    private bool CanIShoot(Unit unit, Game game, Debug debug, Unit? enemy)
    {
        if (enemy.HasValue && unit.Weapon.HasValue)
        {
            if (unit.Position.X > enemy.Value.Position.X)
            {
                var line = new ParametricLine(new PointF((float)unit.Position.X + (float)unit.Size.X, (float)unit.Position.Y), new PointF((float)enemy.Value.Position.X, (float)enemy.Value.Position.Y));
                debug.Draw(new CustomData.Line(new Vec2Float((float)unit.Position.X, (float)unit.Position.Y), new Vec2Float((float)enemy.Value.Position.X, (float)enemy.Value.Position.Y), 0.1f, new ColorFloat(255, 0, 0, 1)));
                var fraction = Math.Abs((int)unit.Position.X) + Math.Abs((int)enemy.Value.Position.X);
                var points = Enumerable.Range(0, fraction)
                    .Select(p => line.Fraction((float)p / fraction));
                foreach (var pointF in points
                    .Where(x =>
                    unit.Weapon.Value.Parameters.Explosion.HasValue
                    && x.X > unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2))
                {
                    debug.Draw(new CustomData.Rect(new Vec2Float(pointF.X, pointF.Y), new Vec2Float((float)unit.Weapon.Value.Parameters.Bullet.Size, (float)unit.Weapon.Value.Parameters.Bullet.Size), new ColorFloat(123, 200, 1, 1)));
                    if (game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Wall
                        || game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.JumpPad)
                        return false;
                }
            }
            else if (unit.Position.X < enemy.Value.Position.X)
            {
                var line = new ParametricLine(new PointF((float)unit.Position.X - (float)unit.Size.X, (float)unit.Position.Y), new PointF((float)enemy.Value.Position.X, (float)enemy.Value.Position.Y));
                debug.Draw(new CustomData.Line(new Vec2Float((float)unit.Position.X, (float)unit.Position.Y), new Vec2Float((float)enemy.Value.Position.X, (float)enemy.Value.Position.Y), 0.1f, new ColorFloat(255, 0, 0, 1)));
                var fraction = Math.Abs((int)unit.Position.X) + Math.Abs((int)enemy.Value.Position.X);
                var points = Enumerable.Range(0, fraction)
                    .Select(p => line.Fraction((float)p / fraction));
                foreach (var pointF in points.Where(x =>
                    unit.Weapon.Value.Parameters.Explosion.HasValue
                    && x.X > unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2))
                {
                    debug.Draw(new CustomData.Rect(new Vec2Float(pointF.X, pointF.Y), new Vec2Float((float)unit.Weapon.Value.Parameters.Bullet.Size, (float)unit.Weapon.Value.Parameters.Bullet.Size), new ColorFloat(123, 200, 1, 1)));
                    if (game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Wall
                        || game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.JumpPad)
                        return false;
                }
            }
        }


        return true;

    }

    private Target GetTarget(CurrentInfo currentInfo, Debug debug)
    {

        if (EnemyBullets.Any(x => x.Line != null))
        {
            var t = EnemyBullets.First(x => x.Line != null);
            var points = t.Line.GetLinePoints(20);

            foreach (var pointF in points)
            {
                debug.Draw(new CustomData.Rect(new Vec2Float(pointF.X, pointF.Y), new Vec2Float(0.2f, 0.2f), new ColorFloat(123, 200, 1, 1)));
            }
        }

        if (!currentInfo.Me.Weapon.HasValue && currentInfo.NearestWeapon.HasValue)
        {

            return new Target
            {
                Position = currentInfo.NearestWeapon.Value.Position,
                Purpose = Purpose.NearestWeapon,
                SwapWeapon = true
            };
        }

        if (currentInfo.Me.Health <= currentInfo.Game.Properties.UnitMaxHealth * 0.8
            && currentInfo.NearestHealth.HasValue)
        {
            if (currentInfo.BestWeapon.HasValue
            && currentInfo.Me.Weapon.Value.Typ != WeaponType.RocketLauncher
            && DistanceSqr(currentInfo.BestWeapon.Value.Position, currentInfo.Me.Position) < 5
            && currentInfo.Me.Health > currentInfo.Game.Properties.UnitMaxHealth * 0.5
            && DistanceSqr(currentInfo.BestWeapon.Value.Position, currentInfo.Me.Position) < DistanceSqr(currentInfo.BestWeapon.Value.Position, currentInfo.NearestHealth.Value.Position))
            {
                return new Target
                {
                    Position = currentInfo.BestWeapon.Value.Position,
                    Purpose = Purpose.BestWeapon,
                    SwapWeapon = true
                };
            }
            else
                return new Target
                {
                    Position = currentInfo.NearestHealth.Value.Position,
                    Purpose = Purpose.Heal
                };

        }

        //if (!currentInfo.NearestHealth.HasValue
        //    && currentInfo.Me.Health < currentInfo.Enemy.Value.Health
        //    && currentInfo.Enemy.Value.Health > currentInfo.Game.Properties.UnitMaxHealth * 0.8
        //    && currentInfo.Me.Health < currentInfo.Game.Properties.UnitMaxHealth * 0.5
        //    && currentInfo.Me.Weapon.HasValue
        //    && currentInfo.Me.Weapon.Value.Typ == WeaponType.RocketLauncher
        //    && (currentInfo.Me.Position.X + currentInfo.Me.Weapon.Value.Parameters.Explosion.Value.Radius * 2 > currentInfo.Enemy.Value.Position.X
        //        || currentInfo.Me.Position.X - currentInfo.Me.Weapon.Value.Parameters.Explosion.Value.Radius * 2 < currentInfo.Enemy.Value.Position.X)
        //    && currentInfo.NearestNotBazuka.HasValue
        //    )
        //{
        //    return new Target
        //    {
        //        Position = currentInfo.NearestNotBazuka.Value.Position,
        //        Purpose = Purpose.NearestNotBazuka,
        //        SwapWeapon = true
        //    };
        //}

        if (currentInfo.BestWeapon.HasValue
            && currentInfo.Me.Weapon.HasValue
            && currentInfo.Me.Weapon.Value.Typ != WeaponType.RocketLauncher)
        {
            return new Target
            {
                Position = currentInfo.BestWeapon.Value.Position,
                Purpose = Purpose.BestWeapon,
                SwapWeapon = true
            };
        }
        if (currentInfo.Enemy.HasValue)
        {
            return new Target
            {
                //+радиус взрыва пули
                Position = currentInfo.Enemy.Value.Position,
                Purpose = Purpose.Enemy
            };
        }

        return new Target
        {
            Position = currentInfo.HomePosition,
            Purpose = Purpose.Home
        };
    }

    private Target NeoModeTarget(Bullet bullet, CurrentInfo currentInfo)
    {
        if (currentInfo.Me.Position.X > bullet.Position.X)
        {
            if (bullet.Position.Y + bullet.Size > currentInfo.Me.Position.Y - 1
                && bullet.Position.Y < currentInfo.Me.Position.Y + currentInfo.Me.Size.Y
                && bullet.Position.X != currentInfo.Me.Position.X)
            {
                return new Target
                {
                    Position = new Vec2Double(currentInfo.Me.Position.X + 5, currentInfo.Me.Position.Y + 10),
                    Purpose = Purpose.NeoMode,
                    NeedJump = true
                };
            }

            if ((int)bullet.Position.Y < (int)currentInfo.Me.Position.Y
                            && bullet.Position.X != currentInfo.Me.Position.X)
            {
                return new Target
                {
                    Position = new Vec2Double(currentInfo.Me.Position.X + 5, currentInfo.Me.Position.Y),
                    Purpose = Purpose.NeoMode,
                    NeedJump = false
                };
            }

            if ((int)bullet.Position.Y > (int)currentInfo.Me.Position.Y
                            && bullet.Position.X != currentInfo.Me.Position.X)
            {
                return new Target
                {
                    Position = new Vec2Double(currentInfo.Me.Position.X + 5, currentInfo.Me.Position.Y),
                    Purpose = Purpose.NeoMode,
                    NeedJump = false
                };
            }

        }
        else
        {
            if (bullet.Position.Y + bullet.Size > currentInfo.Me.Position.Y - 1
                && bullet.Position.Y < currentInfo.Me.Position.Y + currentInfo.Me.Size.Y
                && bullet.Position.X != currentInfo.Me.Position.X)
            {
                return new Target
                {
                    Position = new Vec2Double(currentInfo.Me.Position.X - 5, currentInfo.Me.Position.Y + 10),
                    Purpose = Purpose.NeoMode,
                    NeedJump = true
                };
            }

            if ((int)bullet.Position.Y < (int)currentInfo.Me.Position.Y
                            && bullet.Position.X != currentInfo.Me.Position.X)
            {
                return new Target
                {
                    Position = new Vec2Double(currentInfo.Me.Position.X - 5, currentInfo.Me.Position.Y),
                    Purpose = Purpose.NeoMode,
                    NeedJump = false
                };
            }

            if ((int)bullet.Position.Y > (int)currentInfo.Me.Position.Y
                            && bullet.Position.X != currentInfo.Me.Position.X)
            {
                return new Target
                {
                    Position = new Vec2Double(currentInfo.Me.Position.X - 5, currentInfo.Me.Position.Y),
                    Purpose = Purpose.NeoMode,
                    NeedJump = false
                };
            }
        }

        return new Target
        {
            Position = currentInfo.Me.Position
        };
    }
}