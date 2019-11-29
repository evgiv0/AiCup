﻿using AiCup2019;
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


        LootBox? nearestWeapon = null;
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
            BestWeapon = bestWeapon,
            Game = game
        });


        debug.Draw(new CustomData.Log("Target pos: " + targetPos));

        Vec2Double aim = new Vec2Double(0, 0);
        if (nearestEnemy.HasValue)
        {
            //if(!nearestEnemy.Value.OnGround && nearestEnemy.Value.)
            aim = new Vec2Double(nearestEnemy.Value.Position.X - unit.Position.X, nearestEnemy.Value.Position.Y - unit.Position.Y);
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
            shoot = CanIShoot(unit, aim, game);

        }

        double velocity = targetPos.X - unit.Position.X;
        if (unit.Position.X > targetPos.X && velocity > -1)
            velocity = -10;
        else if (unit.Position.X > nearestEnemy?.Position.X && isBazukaBlya &&
                 unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius - 2 < nearestEnemy?.Position.X)
        {
            jump = true;
            velocity = game.Properties.UnitMaxHorizontalSpeed;
        }
        else if (unit.Position.X < targetPos.X && velocity < 1)
            velocity = 10;
        else if (unit.Position.X < nearestEnemy?.Position.X && isBazukaBlya &&
                 unit.Position.X + unit.Weapon.Value.Parameters.Explosion.Value.Radius + 2 > nearestEnemy?.Position.X)
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

    private bool CanIShoot(Unit unit, Vec2Double aim, Game game)
    {
        var line = new ParametricLine(new PointF((float)unit.Position.X, (float)unit.Position.Y), new PointF((float)aim.X, (float)aim.Y));
        var fraction = Math.Abs((int)unit.Position.X) +  Math.Abs((int)aim.X);
        var points = Enumerable.Range(0, fraction)
            .Select(p => line.Fraction((float)p / fraction));

        if (unit.Position.X > aim.X)
        {
            foreach (var pointF in points.Where(x => x.X > unit.Position.X - unit.Weapon.Value.Parameters.Explosion.Value.Radius + 1))
            {
                if (game.Level.Tiles[(int)pointF.X][(int)pointF.Y] == Tile.Wall)
                    return false;
            }
        }
        else if (unit.Position.X < aim.X)
        {
            foreach (var pointF in points.Where(x => x.X < unit.Position.X + unit.Weapon.Value.Parameters.Explosion.Value.Radius + 1))
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

        if (currentInfo.Me.Health - 10 <= currentInfo.Game.Properties.UnitMaxHealth / 2 && currentInfo.NearestHealth.HasValue)
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