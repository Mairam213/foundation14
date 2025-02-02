﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Containment.Cage;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Storage.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp173;

public abstract class SharedScp173System : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private const float ContainmentRoomSearchRadius = 4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, ComponentInit>(OnInit);

        #region Blocker

        SubscribeLocalEvent((Entity<Scp173Component> _, ref BeforeDamageChangedEvent args) => args.Cancelled = true);
        SubscribeLocalEvent<Scp173Component, AttackAttemptEvent>((uid, component, args) =>
        {
            if (Is173Watched((uid, component), out _))
                args.Cancel();
        });

        #endregion

        #region Movement

        SubscribeLocalEvent<Scp173Component, ChangeDirectionAttemptEvent>(OnDirectionAttempt);
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<Scp173Component, MoveEvent>(OnInput);

        #endregion

        #region Abillities

        SubscribeLocalEvent<Scp173Component, StartCollideEvent>(OnCollide);

        SubscribeLocalEvent<Scp173Component, Scp173BlindAction>(OnBlind);

        #endregion
    }

    private void OnInit(Entity<Scp173Component> ent, ref ComponentInit args)
    {
        // Fallback
        ent.Comp.NeckSnapDamage ??= new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 200);

        Dirty(ent);
    }

    #region Movement

    private void OnDirectionAttempt(Entity<Scp173Component> ent, ref ChangeDirectionAttemptEvent args)
    {
        if (Is173Watched(ent, out _) && !IsInScpCage(ent, out _))
            args.Cancel();
    }

    private void OnMoveAttempt(Entity<Scp173Component> ent, ref UpdateCanMoveEvent args)
    {
        if (Is173Watched(ent, out _) && !IsInScpCage(ent, out _))
            args.Cancel();
    }

    private void OnInput(Entity<Scp173Component> ent, ref MoveEvent args)
    {
        _blocker.UpdateCanMove(ent);
    }

    #endregion

    #region Abillities

    private void OnCollide(Entity<Scp173Component> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        // Мы должны двигаться, чтобы сломать шею
        if (physicsComponent.LinearVelocity.IsLengthZero())
            return;

        BreakNeck(target, ent.Comp);
    }

    private void OnBlind(Entity<Scp173Component> ent, ref Scp173BlindAction args)
    {
        if (args.Handled)
            return;

        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(ent).Coordinates, ExamineSystemShared.MaxRaycastRange);

        foreach (var eye in eyes)
        {
            _blinking.ForceBlind(eye.Owner, eye.Comp, TimeSpan.FromSeconds(6));
        }

        // TODO: Add sound.

        args.Handled = true;
    }

    protected abstract void BreakNeck(EntityUid target, Scp173Component scp);

    #endregion

    #region Helpers

    protected bool Is173Watched(Entity<Scp173Component> scp173, out int watchersCount)
    {
        var eyes = _lookupSystem.GetEntitiesInRange<BlinkableComponent>(Transform(scp173).Coordinates, ExamineSystemShared.MaxRaycastRange);

        watchersCount = eyes
            .Where(eye => _examine.InRangeUnOccluded(eye, scp173, scp173.Comp.WatchRange, ignoreInsideBlocker: false))
            .Count(eye => !IsEyeBlinded(eye, scp173));

        return watchersCount != 0;
    }

    private bool IsEyeBlinded(Entity<BlinkableComponent> eye, EntityUid scpUid)
    {
        if (_mobState.IsIncapacitated(eye))
            return true;

        // Проверка на то, что игрок смотрит на сцп лицом.
        if (IsWithinViewAngle(scpUid, eye, 120f))
            return true;

        if (_blinking.IsBlind(eye.Owner, eye.Comp, true))
            return true;

        var canSeeAttempt = new CanSeeAttemptEvent();
        RaiseLocalEvent(eye, canSeeAttempt);

        if (canSeeAttempt.Blind)
            return true;

        return false;
    }

    private bool IsWithinViewAngle(EntityUid scpEntity, EntityUid targetEntity, float maxAngle)
    {
        var angle = FindAngleBetween(scpEntity, targetEntity);

        // Проверка: угол должен быть больше или равен maxAngle, чтобы SCP был вне поля зрения
        return angle >= maxAngle;
    }

    private float FindAngleBetween(Entity<TransformComponent?> scp, Entity<TransformComponent?> target)
    {
        if (!Resolve<TransformComponent>(scp, ref scp.Comp))
            return float.MaxValue;

        if (!Resolve<TransformComponent>(target, ref target.Comp))
            return float.MaxValue;

        var scpWorldPosition = _transformSystem.GetMoverCoordinates(scp.Owner);
        var targetWorldPosition = _transformSystem.GetMoverCoordinates(target.Owner);

        var toScp = (scpWorldPosition.Position - targetWorldPosition.Position).Normalized(); // Вектор от target к SCP
        var targetForward = target.Comp.LocalRotation.ToWorldVec(); // Направление взгляда target

        // Проверка, смотрит ли цель на SCP или спиной к нему
        var dotProduct = Vector2.Dot(targetForward, toScp);

        // Если цель смотрит спиной (угол > 90 градусов), возвращаем MaxValue
        if (dotProduct < 0)
            return float.MaxValue;

        // Иначе вычисляем угол
        var angle = MathF.Acos(dotProduct) * (180f / MathF.PI);

        return angle;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Находится ли 173 в контейнере для перевозки
    /// </summary>
    public bool IsInScpCage(EntityUid uid, [NotNullWhen(true)] out EntityUid? storage)
    {
        storage = null;

        if (TryComp<InsideEntityStorageComponent>(uid, out var insideEntityStorageComponent) &&
            HasComp<ScpCageComponent>(insideEntityStorageComponent.Storage))
        {
            storage = insideEntityStorageComponent.Storage;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Находится ли 173 в своей камере. Проверяется по наличию рядом спавнера работы
    /// </summary>
    public bool IsContained(EntityUid uid)
    {
        var lookup = _lookup.GetEntitiesInRange(uid, ContainmentRoomSearchRadius);
        return lookup.Any(HasComp<Scp173BlockStructureDamageComponent>);
    }

    #endregion
}
