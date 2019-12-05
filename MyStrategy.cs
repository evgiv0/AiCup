using AiCup2019;
using AiCup2019.Model;
using AiCup2019.OwnModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class MyStrategy
{
    static double DistanceSqr(Vec2Double a, Vec2Double b)
    {
        return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    }

    public bool? InJump { get; set; }
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


        var target = GetTarget(new CurrentInfo
        {
            Me = unit,
            Enemy = nearestEnemy,
            NearestHealth = nearestHealth,
            NearestMine = nearestMine,
            NearestWeapon = nearestWeapon,
            NearestNotBazuka = nearestNotBazuka,
            HomePosition = homePosition,
            Bullets = game.Bullets.Where(b => b.PlayerId != unit.PlayerId
            && Math.Abs(unit.Position.X - b.Position.X) < 5
            && Math.Abs(unit.Position.Y - b.Position.Y) < 5).ToList(),
            BestWeapon = bestWeapon,
            Game = game
        });

        var targetPos = target.Position;

        debug.Draw(new CustomData.Log("Target pos: " + target.Position));

        Vec2Double aim = new Vec2Double(0, 0);
        if (nearestEnemy.HasValue)
        {
            //if(!nearestEnemy.Value.OnGround && nearestEnemy.Value.)
            aim = new Vec2Double(nearestEnemy.Value.Position.X - unit.Position.X, nearestEnemy.Value.Position.Y - unit.Position.Y);
            debug.Draw(new CustomData.Line(new Vec2Float((float)unit.Position.X, (float)unit.Position.Y), new Vec2Float((float)aim.X, (float)aim.Y), 0.1f, new ColorFloat(255, 0, 0, 1)));

        }

        if (target.Purpose == Purpose.NeoMode)
            InJump = target.NeedJump;
        else
            InJump = null;

        bool jump = targetPos.Y > unit.Position.Y;
        if (targetPos.X > unit.Position.X && game.Level.Tiles[(int)(unit.Position.X + 1)][(int)(unit.Position.Y)] == Tile.Wall)
        {
            jump = true;
        }
        else if (targetPos.X < unit.Position.X && game.Level.Tiles[(int)(unit.Position.X - 1)][(int)(unit.Position.Y)] == Tile.Wall)
        {
            jump = true;
        }

        else if (targetPos.Y < unit.Position.Y && game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y - 1)] == Tile.Ladder &&
                                               game.Level.Tiles[(int)(unit.Position.X)][(int)(unit.Position.Y)] != Tile.Ladder)
        {
            jump = false;
        }

        else if ((int)unit.Position.Y == (int)nearestEnemy.Value.Position.Y && Math.Abs((int)unit.Position.X - (int)nearestEnemy.Value.Position.X) < 2)
        {
            jump = true;
        }
        //else if (target.Purpose == Purpose.Heal)
        //    jump = true;

        if (InJump.HasValue)
            jump = InJump.Value;
        bool shoot = true;

        //shoot = IsAnyWalls(unit.Position, aim);
        var isBazukaBlya = unit.Weapon.HasValue && unit.Weapon.Value.Typ == WeaponType.RocketLauncher &&
                           unit.Weapon.Value.Parameters.Explosion.HasValue;
        if (isBazukaBlya)
        {
            shoot = CanIShoot(unit, aim, game, debug, nearestEnemy.Value, nearestHealth);

        }


        double velocity = targetPos.X - unit.Position.X;

        if (unit.Position.X > targetPos.X)
        {
            if (target.Purpose == Purpose.Enemy
                && isBazukaBlya
                && unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius * 3 < nearestEnemy.Value.Position.X
                && Math.Abs(unit.Position.Y - nearestEnemy.Value.Position.Y) < unit.Weapon.Value.Parameters.Explosion.Value.Radius * 3)
            {
                velocity = game.Properties.UnitMaxHorizontalSpeed;
            }

            else
                velocity = -game.Properties.UnitMaxHorizontalSpeed;

        }
        else if (unit.Position.X < targetPos.X)
        {
            if (target.Purpose == Purpose.Enemy
                && isBazukaBlya
                && unit.Position.X + unit.Weapon.Value.Parameters.Explosion.Value.Radius * 3 > nearestEnemy.Value.Position.X
                && Math.Abs(unit.Position.Y - nearestEnemy.Value.Position.Y) < unit.Weapon.Value.Parameters.Explosion.Value.Radius * 3)

            {
                velocity = -game.Properties.UnitMaxHorizontalSpeed;
            }

            else
                velocity = game.Properties.UnitMaxHorizontalSpeed;
        }

        UnitAction action = new UnitAction
        {
            Velocity = velocity,
            Jump = jump,
            JumpDown = !jump,
            Aim = aim,
            Shoot = false,
            Reload = false,
            SwapWeapon = target.SwapWeapon,
            PlantMine = false
        };
        return action;
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

    private bool CanIShoot(Unit unit, Vec2Double aim, Game game, Debug debug, Unit enemy, LootBox? nearestHealth)
    {
        var line = new ParametricLine(new PointF((float)unit.Position.X, (float)unit.Position.Y), new PointF((float)enemy.Position.X, (float)enemy.Position.Y));
        debug.Draw(new CustomData.Line(new Vec2Float((float)unit.Position.X, (float)unit.Position.Y), new Vec2Float((float)aim.X, (float)aim.Y), 0.1f, new ColorFloat(255, 0, 0, 1)));
        var fraction = Math.Abs((int)unit.Position.X) + Math.Abs((int)enemy.Position.X);
        var points = Enumerable.Range(0, fraction)
            .Select(p => line.Fraction((float)p / fraction));

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
                if (game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Wall
                    || game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Platform
                    || game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.JumpPad)
                    return false;
            }
        }


        //if (Math.Abs(unit.Position.X - enemy.Position.X) < unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2
        //    && Math.Abs(unit.Position.Y - enemy.Position.Y) < unit.Weapon.Value.Parameters.Explosion.Value.Radius * 2
        //    && unit.Health > unit.Weapon.Value.Parameters.Explosion.Value.Damage + unit.Weapon.Value.Parameters.FireRate
        //    && nearestHealth.HasValue
        //    && (int)nearestHealth.Value.Position.X != (int)enemy.Position.X)
        //    return true;

        return true;

    }

    private Target GetTarget(CurrentInfo currentInfo)
    {
        Bullet? nearestBullet = null;
        if (currentInfo.Bullets.Any())
        {
            var minDistanse = currentInfo.Bullets.Min(x => DistanceSqr(x.Position, currentInfo.Me.Position));
            nearestBullet = currentInfo.Bullets.FirstOrDefault(x => DistanceSqr(x.Position, currentInfo.Me.Position) == minDistanse);
        }
            
        if (nearestBullet != null)
        {
            return NeoModeTarget(nearestBullet.Value, currentInfo);
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

        if (currentInfo.Me.Health <= currentInfo.Game.Properties.UnitMaxHealth * 0.9 && currentInfo.NearestHealth.HasValue)
        {
            return new Target
            {
                Position = currentInfo.NearestHealth.Value.Position,
                Purpose = Purpose.Heal
            };
        }

        if (!currentInfo.NearestHealth.HasValue
            && currentInfo.Me.Health < currentInfo.Enemy.Value.Health
            && currentInfo.Me.Health < currentInfo.Game.Properties.UnitMaxHealth * 0.5
            && currentInfo.Me.Weapon.HasValue
            && currentInfo.Me.Weapon.Value.Typ == WeaponType.RocketLauncher
            && (currentInfo.Me.Position.X + currentInfo.Me.Weapon.Value.Parameters.Explosion.Value.Radius * 2 > currentInfo.Enemy.Value.Position.X
                || currentInfo.Me.Position.X - currentInfo.Me.Weapon.Value.Parameters.Explosion.Value.Radius * 2 < currentInfo.Enemy.Value.Position.X)
            && currentInfo.NearestNotBazuka.HasValue
            )
        {
            return new Target
            {
                Position = currentInfo.NearestNotBazuka.Value.Position,
                Purpose = Purpose.NearestNotBazuka,
                SwapWeapon = true
            };
        }

        if (currentInfo.BestWeapon.HasValue
            && currentInfo.Me.Weapon.HasValue
            && currentInfo.Me.Weapon.Value.Typ != WeaponType.RocketLauncher
            && currentInfo.Me.Health > currentInfo.Game.Properties.UnitMaxHealth * 0.5)
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
            if (bullet.Position.Y + bullet.Size > currentInfo.Me.Position.Y + currentInfo.Me.Size.Y - 1
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