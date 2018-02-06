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
		ofMesh	mesh;
		ofEasyCam	camera;

};
