// MIT License - Copyright (c) 2026 wallstop
// Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

// Proper hash code implementation using Objects.HashCode for deterministic hashing
// NEVER use System.HashCode or hand-rolled hash patterns

namespace WallstopStudios.UnityHelpers.Examples
{
    using System;
    using WallstopStudios.UnityHelpers.Core.Helper;

    // ============================================================================
    // WRONG: System.HashCode.Combine - Non-deterministic across processes
    // ============================================================================

    // public struct BadPlayerData
    // {
    //     public string Name;
    //     public int Score;
    //
    //     // FORBIDDEN - hash changes between process restarts!
    //     public override int GetHashCode() => HashCode.Combine(Name, Score);
    // }

    // ============================================================================
    // WRONG: Hand-rolled hash patterns - Error-prone and non-deterministic
    // ============================================================================

    // public struct BadPlayerDataHandRolled
    // {
    //     public string Name;
    //     public int Score;
    //
    //     // FORBIDDEN - * 31 pattern is non-deterministic for strings
    //     public override int GetHashCode()
    //     {
    //         int hash = 17;
    //         hash = hash * 31 + (Name?.GetHashCode() ?? 0);
    //         hash = hash * 31 + Score.GetHashCode();
    //         return hash;
    //     }
    // }

    // public struct BadPlayerDataXor
    // {
    //     public string Name;
    //     public int Score;
    //
    //     // FORBIDDEN - XOR has poor distribution
    //     public override int GetHashCode() => (Name?.GetHashCode() ?? 0) ^ Score;
    // }

    // ============================================================================
    // CORRECT: Objects.HashCode - Deterministic across processes and platforms
    // ============================================================================

    /// <summary>
    /// Example struct demonstrating proper hash code implementation.
    /// </summary>
    public readonly struct PlayerData : IEquatable<PlayerData>
    {
        public readonly string Name;
        public readonly int Score;
        public readonly float Multiplier;

        public PlayerData(string name, int score, float multiplier)
        {
            Name = name;
            Score = score;
            Multiplier = multiplier;
        }

        // Correct: Use Objects.HashCode for deterministic hashing
        public override int GetHashCode()
        {
            return Objects.HashCode(Name, Score, Multiplier);
        }

        public bool Equals(PlayerData other)
        {
            return Name == other.Name && Score == other.Score && Multiplier == other.Multiplier;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerData other && Equals(other);
        }

        public static bool operator ==(PlayerData left, PlayerData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerData left, PlayerData right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Example class demonstrating proper hash code implementation with nullable fields.
    /// </summary>
    public sealed class GameSession : IEquatable<GameSession>
    {
        private readonly string _sessionId;
        private readonly PlayerData _playerData;
        private readonly DateTime _startTime;

        public GameSession(string sessionId, PlayerData playerData, DateTime startTime)
        {
            _sessionId = sessionId;
            _playerData = playerData;
            _startTime = startTime;
        }

        // Correct: Objects.HashCode handles null values safely
        public override int GetHashCode()
        {
            return Objects.HashCode(_sessionId, _playerData, _startTime);
        }

        public bool Equals(GameSession other)
        {
            if (other is null)
            {
                return false;
            }

            return _sessionId == other._sessionId
                && _playerData.Equals(other._playerData)
                && _startTime == other._startTime;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GameSession);
        }
    }

    /// <summary>
    /// Example with cached hash code for immutable types used frequently in lookups.
    /// </summary>
    public readonly struct CachedHashExample : IEquatable<CachedHashExample>
    {
        private readonly string _key;
        private readonly int _value;
        private readonly int _cachedHash;

        public CachedHashExample(string key, int value)
        {
            _key = key;
            _value = value;
            // Pre-compute hash for frequently-accessed immutable types
            _cachedHash = Objects.HashCode(key, value);
        }

        // Return cached hash - zero cost after construction
        public override int GetHashCode()
        {
            return _cachedHash;
        }

        public bool Equals(CachedHashExample other)
        {
            return _key == other._key && _value == other._value;
        }

        public override bool Equals(object obj)
        {
            return obj is CachedHashExample other && Equals(other);
        }

        public static bool operator ==(CachedHashExample left, CachedHashExample right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CachedHashExample left, CachedHashExample right)
        {
            return !left.Equals(right);
        }
    }
}
