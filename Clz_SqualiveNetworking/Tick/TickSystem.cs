// This file is provided under The MIT License as part of SqualiveNetworking.
// Copyright (c) Squalive-Studios
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/Squalive/SqualiveNetworking

using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace SqualiveNetworking.Tick
{
    public unsafe struct TickSystem
    {
        private readonly uint _tickRate;

        private readonly float _tickTime;

        private readonly int _tickTolorance;

        private readonly float _multiplayer;

        private bool _smooth;

        private float _currentTime;

        private uint _currentTick;

        private float _offsetDt;

        public uint CurrentTick => _currentTick;

        public TickSystem( bool smooth, uint tickRate, int tickTolorance = 1, float multiplayer = 3f )
        {
            _smooth = smooth;
            
            _tickRate = tickRate;

            _tickTime = 1f / tickRate;

            _tickTolorance = tickTolorance;

            _multiplayer = multiplayer;

            _currentTime = 0;

            _currentTick = 0;

            _offsetDt = 0;
        }

        public TickSystem( uint tickRate ) : this( false, tickRate, 0, 0 )
        {
            
        }

        /// <summary>
        /// Yippe
        /// </summary>
        /// <param name="deltaTime">The time between last frame to this frame</param>
        /// <returns></returns>
        public bool Update( ref float deltaTime )
        {
            _currentTime += deltaTime * GetOffset();

            deltaTime = 0;

            if ( _currentTime >= _tickTime )
            {
                _currentTime -= _tickTime;

                _currentTick++;
                
                return true;
            }

            return false;
        }

        public void Sync( uint tick )
        {
            int tolorance = (int)( tick - _currentTick );
            
            if ( math.abs( tolorance ) >= _tickTolorance )
            {
                if ( _smooth )
                {
                    _offsetDt = tolorance * _tickTime * _multiplayer;
                }
                else
                {
                    _currentTick = tick;
                }
            }
        }

        private float GetOffset()
        {
            if ( _offsetDt > 0 )
            {
                _offsetDt -= _tickTime;

                if ( _offsetDt < 0 )
                    _offsetDt = 0;

                return 2f * _multiplayer;
            }
            
            if( _offsetDt < 0 )
            {
                _offsetDt += _tickTime;
                
                if ( _offsetDt > 0 )
                    _offsetDt = 0;
                
                return -2f * _multiplayer;
            }

            return 1;
        }
    }
}