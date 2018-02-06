#pragma once

#include "ofMain.h"
#include "ofxKinectCommonBridge.h"

class testApp : public ofBaseApp{
	public:
		void setup();
		void update();
		void draw();
		
		void updateMesh();
		void drawMesh();

		ofxKinectCommonBridge	kinect;

		ofFloatPixels			pxlDepth;
		ofPixels				pxlBodyIndex;
		ofPixels				pxlColorMappedToDepth;
		ofTexture				texColorMappedToDepth;

		vector <ofVec3f>		points;
		vector <ofVec3f>		sizes;
		vector <ofFloatColor>	colors;

		ofVbo					vbo;
		ofEasyCam				camera;

		ofPixels				pxlsMappedColor;

		ofTexture				texture;
		ofShader				shader;
};
