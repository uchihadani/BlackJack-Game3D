using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TwentyThree.Domain.Content;
using TwentyThree.Domain.Economy;

namespace TwentyThree.Domain.Events
{
    public sealed class GameEventDefinition
    {
        private readonly ReadOnlyCollection<ContentTargetKind> _targets;
        private readonly ReadOnlyCollection<EffectCategory> _effectCategories;

        public GameEventDefinition(
            GameEventId id,
            string displayName,
            BasisPoints baseProbability,
            bool canReject,
            Money rejectionCost,
            int pressureDelta,
            int lucidityDelta,
            ContentDuration duration,
            IReadOnlyList<ContentTargetKind> targets,
            IReadOnlyList<EffectCategory> effectCategories)
        {
            if (!Enum.IsDefined(typeof(GameEventId), id))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("A display name is required.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(ContentDuration), duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Id = id;
            DisplayName = displayName;
            BaseProbability = baseProbability;
            CanReject = canReject;
            RejectionCost = rejectionCost;
            PressureDelta = pressureDelta;
            LucidityDelta = lucidityDelta;
            Duration = duration;
            _targets = Array.AsReadOnly(CopyAndValidate(targets, nameof(targets)));
            _effectCategories = Array.AsReadOnly(
                CopyAndValidate(effectCategories, nameof(effectCategories)));
        }

        public GameEventId Id { get; }

        public string DisplayName { get; }

        public BasisPoints BaseProbability { get; }

        public bool CanReject { get; }

        public Money RejectionCost { get; }

        public int PressureDelta { get; }

        public int LucidityDelta { get; }

        public ContentDuration Duration { get; }

        public IReadOnlyList<ContentTargetKind> Targets => _targets;

        public IReadOnlyList<EffectCategory> EffectCategories => _effectCategories;

        private static T[] CopyAndValidate<T>(IReadOnlyList<T> values, string parameterName)
            where T : struct
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (values.Count == 0)
            {
                throw new ArgumentException("At least one value is required.", parameterName);
            }

            T[] copy = new T[values.Count];
            HashSet<T> unique = new HashSet<T>();
            for (int index = 0; index < values.Count; index++)
            {
                T value = values[index];
                if (!Enum.IsDefined(typeof(T), value))
                {
                    throw new ArgumentOutOfRangeException(parameterName);
                }

                if (!unique.Add(value))
                {
                    throw new ArgumentException("Duplicate values are not allowed.", parameterName);
                }

                copy[index] = value;
            }

            return copy;
        }
    }
}
