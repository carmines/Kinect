
#version 150

/*
 * Copyright 2009, Google Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 *     * Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above
 * copyright notice, this list of conditions and the following disclaimer
 * in the documentation and/or other materials provided with the
 * distribution.
 *     * Neither the name of Google Inc. nor the names of its
 * contributors may be used to endorse or promote products derived from
 * this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
 * OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
 
 /* mod 2014 joshua noble */
 
// This shader takes a Y'UV420p image as a single greyscale plane, and
// converts it to RGB by sampling the correct parts of the image, and 
// by converting the colorspace to RGB on the fly. 

// This is the texture sampler where the greyscale Y'UV420p image is
// accessed.
uniform sampler2D textureSampler;

in vec2 texCoordVarying;
out vec4 color;

float getYPixel(vec2 position) {
  position.y = (position.y * 2.0 / 3.0) + (1.0 / 3.0);
  return texture2D(textureSampler, position).x;
}

vec2 mapCommon(vec2 position, float planarOffset) {
  planarOffset += (imageWidth * floor(position.y / 2.0)) / 2.0 +
                  floor((imageWidth - 1.0 - position.x) / 2.0);
  float x = floor(imageWidth - 1.0 - floor(mod(planarOffset, imageWidth)));
  float y = floor(floor(planarOffset / imageWidth));
  return vec2((x + 0.5) / imageWidth, (y + 0.5) / (1.5 * imageHeight));
}

vec2 mapU(vec2 position) {
  float planarOffset = (imageWidth * imageHeight) / 4.0;
  return mapCommon(position, planarOffset);
}

vec2 mapV(vec2 position) {
  return mapCommon(position, 0.0);
}

void main() {
  // Calculate what image pixel we're on, since we have to calculate
  // the location in the image stream, using floor in several places
  // which makes it hard to use parametric coordinates.
  vec2 pixelPosition = vec2(floor(imageWidth * v_texcoord.x),
                                floor(imageHeight * v_texcoord.y));
  pixelPosition -= vec2(0.5, 0.5);
  // We can use the parametric coordinates to get the Y channel, since it's
  // a relatively normal image.
  float yChannel = getYPixel(v_texcoord);

  // As noted above, the U and V planes are smashed onto the end of
  // the image in an odd way (in our 2D texture mapping, at least), so
  // these mapping functions take care of that oddness.
  float uChannel = texture2D(textureSampler, mapU(pixelPosition)).x;
  float vChannel = texture2D(textureSampler, mapV(pixelPosition)).x;

  // This does the colorspace conversion from Y'UV to RGB as a matrix
  // multiply.  It also does the offset of the U and V channels from
  // [0,1] to [-.5,.5] as part of the transform.
  vec4 channels = vec4(yChannel, uChannel, vChannel, 1.0);

  mat4 conversion = mat4(1.0,  0.0,    1.402, -0.701,
                         1.0, -0.344, -0.714,  0.529,
                         1.0,  1.772,  0.0,   -0.886,
                         0, 0, 0, 0);
  vec3 rgb = (channels * conversion).xyz;

  // This is another Y'UV transform that can be used, but it doesn't
  // accurately transform my source image.  Your images may well fare
  // better with it, however, considering they come from a different
  // source, and because I'm not sure that my original was converted
  // to Y'UV420p with the same RGB->YUV (or YCrCb) conversion as
  // yours.
  //
  // vec4 channels = vec4(yChannel, uChannel, vChannel, 1.0);
  // float3x4 conversion = float3x4(1.0,  0.0,      1.13983, -0.569915,
  //                                1.0, -0.39465, -0.58060,  0.487625,
  //                                1.0,  2.03211,  0.0,     -1.016055);
  // float3 rgb = mul(conversion, channels);

  // Note: The output cannot fully replicate the original image. This is partly
  // because WebGL has limited NPOT (non-power-of-two) texture support and also
  // due to sRGB color conversions that occur in WebGL but not in the plugin.
  color = vec4(rgb, 1.0);
}
