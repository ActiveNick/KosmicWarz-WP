#region File Description
//-----------------------------------------------------------------------------
// Sky.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace SpaceInvadersWP7
{
    /// <summary>
    /// Runtime class for loading and rendering a textured skydome
    /// that was created during the build process by the SkyProcessor.
    /// </summary>
    public class Sky
    {
        #region Fields

        public Model Model;
        public Texture2D Texture;


        // The sky texture is a cylinder, so it should wrap from left to right,
        // but not from top to bottom. This requires a custom sampler state object.
        static readonly SamplerState WrapUClampV = new SamplerState
        {
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Clamp,
        };

        #endregion


        /// <summary>
        /// Helper for drawing the skydome mesh.
        /// </summary>
        public void Draw(Camera cam)
        {
            GraphicsDevice device = Texture.GraphicsDevice;
            Matrix view = cam.View;
            Matrix projection = cam.projectionMatrix;

            // Set renderstates for drawing the sky. For maximum efficiency, we draw the sky
            // after everything else, with depth mode set to read only. This allows the GPU to
            // entirely skip drawing sky in the areas that are covered by other solid objects.
            device.DepthStencilState = DepthStencilState.DepthRead;
            device.SamplerStates[0] = WrapUClampV;
            device.BlendState = BlendState.Opaque;

            // Because the sky is infinitely far away, it should not move sideways as the camera
            // moves around the world, so we force the view matrix translation to zero. This
            // way the sky only takes the camera rotation into account, ignoring its position.
            // The only exception is when we were hit and the camera is shaking, we need everything
            // to shake, including the sky, for enhanced effect
            if (cam.shaking)
            {
                view.Translation = cam.shakeOffset * 0.005f;
            }
            else
            {
                view.Translation = Vector3.Zero;
            }

            // The sky should be drawn behind everything else, at the far clip plane.
            // We achieve this by tweaking the projection matrix to force z=w.
            projection.M13 = projection.M14;
            projection.M23 = projection.M24;
            projection.M33 = projection.M34;
            projection.M43 = projection.M44;

            // Draw the sky model.
            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.View = view;
                    effect.Projection = projection;
                    effect.Texture = Texture;
                    effect.TextureEnabled = true;
                }

                mesh.Draw();
            }

            // Set modified renderstates back to their default values.
            device.DepthStencilState = DepthStencilState.Default;
            device.SamplerStates[0] = SamplerState.LinearWrap;
        }
    }
}
