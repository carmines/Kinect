#include "testApp.h"

#define DEPTH_WIDTH			512
#define DEPTH_HEIGHT		424
#define Z_AXIS_MAGNITUDE	500.0

//--------------------------------------------------------------
void testApp::setup(){

	// INITIALIZE THE SENSOR
	kinect.initSensor();

	// INITIALIZE THE DEPTH STREAM
	kinect.initDepthStream(true);

	// START THE COMMON BRIDGE
	kinect.start();

	// DEFINE A POINT MESH
	mesh.setMode(OF_PRIMITIVE_POINTS);
	mesh.enableColors();

}

//--------------------------------------------------------------
void testApp::update(){
	kinect.update();
	updateMesh();
}

//--------------------------------------------------------------
void testApp::updateMesh() 
{
	// GRAB THE PROCESSED DEPTH PIXELS
	pxlDepth = kinect.getDepthPixelsRef();
	// RECREATE THE MESH EVERY UPDATE
	mesh.clear();
		
	// CREATE A BASIC POINT CLOUD BY SETTING THE POINT Z VALUE TO THE PROCESSED
	// GREYSCALE COLOR OF THE DEPTH IMAGE PER EACH X & Y PIXEL VALUE.
	for (int y = 0; y < DEPTH_HEIGHT; y++) {
		for (int x = 0; x < DEPTH_WIDTH; x++) {
			int index = x + (y*DEPTH_WIDTH);
			float z = Z_AXIS_MAGNITUDE * pxlDepth.getPixels()[index];

			// IGNORE THE HIGH AND LOW VALUES
			if ((z == 0) || (z == Z_AXIS_MAGNITUDE) ) continue;

			// CREATE A POINT WITH THE SAME X AND Y BY A Z VALUE MATCHING THE DEPTH
			ofVec3f newPoint(x - DEPTH_WIDTH / 2, (DEPTH_HEIGHT / 2) - y, z);
			mesh.addVertex(newPoint);

			// TO KEEP IT WHITE, SIMPLY ADD A SOLID WHITE COLOR:
			// mesh.addColor(255);

			// BUT WE CAN ADD SOME SIMPLE DIMENSIONAL GRADIENTS FOR EFFECT
			float redShift = 255.0f * (x / (float) DEPTH_WIDTH);
			float blueShift = 255.0f * (y / (float) DEPTH_HEIGHT);
			float greenShift = 255.0f * (z / Z_AXIS_MAGNITUDE);
			ofColor c = ofColor(int(redShift), greenShift, blueShift);			
			mesh.addColor(c);
		}
	}

}

//--------------------------------------------------------------
void testApp::drawMesh() {
	camera.begin();
	mesh.draw();
	camera.end();
}

//--------------------------------------------------------------
void testApp::draw()
{
	ofBackground(64);
	
	ofSetColor(255);
	kinect.drawDepth(ofRectangle(0,0,DEPTH_WIDTH/2,DEPTH_HEIGHT/2));

	drawMesh();
}
