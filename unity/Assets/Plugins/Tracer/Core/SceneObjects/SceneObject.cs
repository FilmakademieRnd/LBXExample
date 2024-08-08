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


//! @file "SceneObject.cs"
//! @brief Implementation of the TRACER SceneObject, connecting Unity and TRACER functionalty.
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 01.03.2022

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace tracer
{
    //!
    //! Implementation of the TRACER SceneObject, connecting Unity and TRACER functionalty 
    //! around 3D scene specific objects.
    //!
    [DisallowMultipleComponent]
    public class SceneObject : ParameterObject{

        //!
        //! Is the sceneObject locked?
        //!
        public bool _lock = false;
        //!
        //! Position of the SceneObject
        //!
        protected Parameter<Vector3> position;
        //!
        //! Rotation of the SceneObject
        //!
        protected Parameter<Quaternion> rotation;
        //!
        //! Scale of the SceneObject
        //!
        protected Parameter<Vector3> scale;

        //!
        //! Cache the transform component - its a bit faster than .transform
        //!
        protected Transform tr;
        //!
        //! Get the transform
        //!
        public Transform getTr{ get => tr; }

        public bool IsLocked(){ return _lock; }
        
        
        //### EVENTS
        [HideInInspector] public UnityEvent onLockStateChanged;
        //---

        //!
        //! Factory to create a new SceneObject and do it's initialisation.
        //! Use this function instead GameObject.AddComponen<>!
        //!
        //! @param gameObject The gameObject the new SceneObject will be attached to.
        //! @sceneID The scene ID for the new SceneObject.
        //!
        public static new SceneObject Attach(GameObject gameObject, byte sceneID = 254)
        {
            SceneObject obj = gameObject.AddComponent<SceneObject>();
            obj.Init(sceneID);

            return obj;
        }
        //!
        //! Initialisation
        //!
        public override void Awake()
        {
            base.Awake();

            tr = GetComponent<Transform>();

            position = new Parameter<Vector3>(tr.position, "position", this);
            position.hasChanged += updatePosition;
            rotation = new Parameter<Quaternion>(tr.rotation, "rotation", this);
            rotation.hasChanged += updateRotation;
            scale = new Parameter<Vector3>(tr.localScale, "scale", this);
            scale.hasChanged += updateScale;

        }

        //!
        //! Function called, when Unity emit it's OnDestroy event.
        //!
        public virtual void OnDestroy()
        {
            position.hasChanged -= updatePosition;
            rotation.hasChanged -= updateRotation;
            scale.hasChanged -= updateScale;
        }

        //!
        //! Function to lock or unlock the SceneObject.
        //!
        public virtual void lockObject(bool l)
        {
            SceneManager sceneManager = core.getManager<SceneManager>();
            if (l){
                sceneManager.LockSceneObject(this);
                _lock = !l;
                //Debug.Log("locked: " + _lock + " for " + this.name);
            }else{
                sceneManager.UnlockSceneObject(this);
                //Debug.Log("UNLOCK " + _lock + " for " + this.name);
            }
        }

        public virtual void SetLock(bool l)
        {
            _lock = l;
        }

        public void lockObjectLocal(bool l)
        {
            _lock = l;
        }


        //!
        //! Function that emits the scene objects hasChanged event. (Used for parameter updates)
        //!
        //! @param parameter The parameter that has changed. 
        //!
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void emitHasChanged (AbstractParameter parameter)
        {
            if (!_lock)
                base.emitHasChanged(parameter);
        }

        //!
        //! Update GameObject local position.
        //! @param   sender     Object calling the update function
        //! @param   a          new position value
        //!
        protected virtual void updatePosition(object sender, Vector3 a)
        {
            tr.position = a;
            emitHasChanged((AbstractParameter)sender);
        }

        //!
        //! Update GameObject local rotation.
        //! @param   sender     Object calling the update function
        //! @param   a          new rotation value
        //!
        protected virtual void updateRotation(object sender, Quaternion a)
        {
            tr.rotation = a;
            emitHasChanged((AbstractParameter)sender);
        }

        //!
        //! Update GameObject local scale.
        //! @param   sender     Object calling the update function
        //! @param   a          new scale value
        //!
        protected virtual void updateScale(object sender, Vector3 a)
        {
            tr.localScale = a;
            emitHasChanged((AbstractParameter)sender);
        }


        //!
        //! Update is called once per frame
        //!
        protected virtual void Update(){
            updateSceneObjectTransform();
        }
        //!
        //! updates the scene objects transforms and informs all connected parameters about the change
        //!
        private void updateSceneObjectTransform()
        {
            if (tr.position != position.value)
            {
                position.setValue(tr.position, false);
                emitHasChanged(position);
            }
            if (tr.rotation != rotation.value)
            {
                rotation.setValue(tr.rotation, false);
                emitHasChanged(rotation);
            }
            if (tr.localScale != scale.value)
            {
                scale.setValue(tr.localScale, false);
                emitHasChanged(scale);
            }
        }
        
        public virtual void Setup(byte sID = 254, short oID = -1)
        {
            _core.removeParameterObject(this);

            _sceneID = sID;

            if (oID == -1)
                _id = getSoID();
            else
                _id = oID;

            _core.addParameterObject(this);
        }

        public void EmitDespiteLock(AbstractParameter parameter){
            base.emitHasChanged(parameter);
        }

    }
}
