/*
TRACER FOUNDATION -
Toolset for Realtime Animation, Collaboration & Extended Reality
tracer.research.animationsinstitut.de
https://github.com/FilmakademieRnd/TRACER

Copyright (c) 2023 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Lab

TRACER is a development by Filmakademie Baden-Wuerttemberg, Animationsinstitut
R&D Labs in the scope of the EU funded project MAX-R (101070072) and funding on
the own behalf of Filmakademie Baden-Wuerttemberg.  Former EU projects Dreamspace
(610005) and SAUCE (780470) have inspired the TRACER development.
This program is distributed in the hope that it will be useful, but WITHOUT
ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
FOR A PARTICULAR PURPOSE. See the MIT License for more details.
You should have received a copy of the MIT License along with this program;
if not go to https://opensource.org/licenses/MIT
*/


//! @file "SceneDataDefinition.cs"
//! @brief definition of TRACER helpers class.
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 02.03.2022

using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace tracer
{
    //!
    //! Implementation for TRACER helpers.
    //!
    public static class Helpers
    {
        //!
        //! Types for the debug message logs
        //!
        public enum logMsgType
        {
            NONE,
            WARNING,
            ERROR
        }

        //!
        //! Function for message loggin in TRACER
        //! @param objName the name of the script or object sending the message
        //! @param msg the message to be logged
        //!
        public static void Log(string msg, logMsgType type = 0)
        {
            string log = "TRACER: " + msg;

            switch (type)
            {
                case logMsgType.WARNING:
                    Debug.LogWarning(log);
                    break;
                case logMsgType.ERROR:
                    Debug.LogError(log);
                    break;
                default:
                    Debug.Log(log);
                    break;
            }
        }
        //!
        //! Searches and returns types in an assembly.
        //! @param  appDomain   domain to be searched in for assemblies
        //! @param  type    type to be searched
        //! @return array of found types
        //!
        public static Type[] GetAllTypes(AppDomain appDomain, Type type)
        {
            var result = new List<Type>();
            var assemblies = appDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var t in types)
                {
                    if (t.IsSubclassOf(type))
                        result.Add(t);
                }
            }
            return result.ToArray();
        }
        //!
        //! Searches and returns first element of a given list within another list.
        //! @param  a The list containing elements to be searched for.
        //! @param  b The list to be searched in.
        //! @return The first element found.
        //!
        public static T FindFirst<T>(T[] a, T[] b) 
        {
            T newType = default(T);
            foreach (T t1 in a)
            {
                foreach (T t2 in b)
                {
                    if (t1.Equals(t2))
                    {
                        newType = t2;
                        break;
                    }
                }
            }
            return newType;
        }
        //!
        //! Searches and returns a child transform in a tree of transforms by name
        //!
        //! @param aParent The Transform of the parent game object.
        //! @param name The name of the child to be searched for.
        //! @return Retuens the transform of the child if exist, null otherwise. 
        //!
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(aParent);
            while (queue.Count > 0)
            {
                var c = queue.Dequeue();
                if (c.name == aName)
                    return c;
                foreach (Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }
        //!
        //! Copys a byte array into another.
        //! This function is faster for array sizes < 20 bytes!
        //!
        //! @param src The source array.
        //! @param srcOffset The index offset within the source array.        
        //! @param dst The destination array.
        //! @param srcOffset The index offset within the destination array.
        //! @param length The length of the byte array to be copied.
        //!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void copyArray(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
        {
            for (int i = 0; i < length; i++)
                dst[dstOffset + i] = src[srcOffset + i];
        }
        //!
        //! Copys a byte array into another using spans.
        //! This function is faster for array sizes < 20 bytes!
        //!
        //! @param src The source span.
        //! @param srcOffset The index offset within the source span.        
        //! @param dst The destination span.
        //! @param srcOffset The index offset within the destination array.
        //! @param length The length of the span to be copied.
        //!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void copyArray(Span<byte> src, int srcOffset, Span<byte> dst, int dstOffset)
        {
            for (int i = 0; i < src.Length; i++)
                dst[dstOffset + i] = src[srcOffset + i];
        }
        
        //!
        //! remove all the null characters from a given string
        //!
        //! @param input is the string that contains null parameter.
        //!
        public static string RemoveNullCharacters(string input)
        {
            return input.Replace("\0", "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DeltaTime(int startTime, int endTime, int length)
        {
            return Mathf.Min( 
                mod((startTime - endTime), length), 
                mod((endTime - startTime), length) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int mod(float a, float b)
        {
            return Mathf.RoundToInt(a - b * Mathf.Floor(a / b));
        }
    }
}
