using System;
using UnityEngine;
using ZombieApocalypse.Inventory;

namespace ZombieApocalypse.Missions
{
    public enum ObjectiveType
    {
        ReachLocation,
        CollectItem,
        KillZombies,
        Interact,
        Extraction
    }

    public enum ObjectiveState
    {
        Incomplete,
        Complete
    }

    /// <summary>
    /// Serialized objective definition representing a single task in a mission.
    /// Tracks progress, state, targets, and objective UI format.
    /// </summary>
    [Serializable]
    public class Objective
    {
        public string objectiveId = "obj_reach_med";
        public string description = "Reach Medical Building";
        public ObjectiveType type = ObjectiveType.ReachLocation;
        public ObjectiveState state = ObjectiveState.Incomplete;

        [Header("Target Configurations")]
        public string targetLocationId = "loc_med_building";
        public ItemData targetItem;
        public string targetInteractableId;
        public int requiredAmount = 1;
        public int currentAmount = 0;

        [Header("World Marker Target")]
        public Vector3 targetWorldPosition;
        public Transform targetTransform;

        public bool IsCompleted => state == ObjectiveState.Complete || (IsCountBased && currentAmount >= requiredAmount);

        public bool IsCountBased => type == ObjectiveType.KillZombies || type == ObjectiveType.CollectItem;

        public string GetProgressString()
        {
            if (IsCompleted) return $"{description} (Completed)";
            if (IsCountBased && requiredAmount > 1)
            {
                return $"{description} ({Mathf.Min(currentAmount, requiredAmount)} / {requiredAmount})";
            }
            return description;
        }

        public Objective Clone()
        {
            return new Objective
            {
                objectiveId = this.objectiveId,
                description = this.description,
                type = this.type,
                state = this.state,
                targetLocationId = this.targetLocationId,
                targetItem = this.targetItem,
                targetInteractableId = this.targetInteractableId,
                requiredAmount = this.requiredAmount,
                currentAmount = this.currentAmount,
                targetWorldPosition = this.targetWorldPosition,
                targetTransform = this.targetTransform
            };
        }
    }
}
