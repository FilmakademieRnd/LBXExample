﻿/*
-------------------------------------------------------------------------------
VPET - Virtual Production Editing Tools
vpet.research.animationsinstitut.de
https://github.com/FilmakademieRnd/VPET
 
Copyright (c) 2022 Filmakademie Baden-Wuerttemberg, Animationsinstitut R&D Lab
 
This project has been initiated in the scope of the EU funded project 
Dreamspace (http://dreamspaceproject.eu/) under grant agreement no 610005 2014-2016.
 
Post Dreamspace the project has been further developed on behalf of the 
research and development activities of Animationsinstitut.
 
In 2018 some features (Character Animation Interface and USD support) were
addressed in the scope of the EU funded project  SAUCE (https://www.sauceproject.eu/) 
under grant agreement no 780470, 2018-2022
 
VPET consists of 3 core components: VPET Unity Client, Scene Distribution and
Syncronisation Server. They are licensed under the following terms:
-------------------------------------------------------------------------------
*/

//! @file "SceneObjectPointLight.cs"
//! @brief implementation SceneObjectPointLight as a specialisation of the light object.
//! @author Simon Spielmann
//! @author Jonas Trottnow
//! @version 0
//! @date 01.03.2022

using UnityEngine;

namespace tracer
{
    //!
    //! Implementation of the VPET point light object as a specialisation of the light object
    //!
    public class SceneObjectPointLight : SceneObjectLight
    {
        //!
        //! the range of the light
        //!
        public Parameter<float> range;

        // Start is called before the first frame update
        public override void Awake()
        {
            base.Awake();

            _light = this.GetComponent<Light>();
            if (_light)
            {
                range = new Parameter<float>(_light.range, "range", this);
                range.hasChanged += updateRange;
            }
            else
                Helpers.Log("no light component found!");

        }


        //!
        //! Function called, when Unity emit it's OnDestroy event.
        //!
        public override void OnDestroy()
        {
            base.OnDestroy();
            range.hasChanged -= updateRange;
        }

        //!
        //! Update the light range of the GameObject.
        //! @param   sender     Object calling the update function
        //! @param   a          new range value
        //!
        private void updateRange(object sender, float a)
        {
            _light.range = a;
            emitHasChanged((AbstractParameter)sender);
        }

        // Update is called once per frame
        public void Update()
        {
            base.Update();
#if UNITY_EDITOR
            updateSceneObjectLightParameters();
#endif
        }

        //!
        //! updates the Unity light component specific parameters and informs all connected VPET parameters about the change
        //!
        private void updateSceneObjectLightParameters()
        {
            if (_light.range != range.value)
                range.value = _light.range;
        }
    }
}
