#include "testApp.h"

#define DEPTH_WIDTH			512
#define DEPTH_HEIGHT		424
#define Z_AXIS_MAGNITUDE	800.0

//--------------------------------------------------------------
void testApp::setup(){

	// INITIALIZE THE SENSOR
	kinect.initSensor();

	// INITIALIZE THE DEPTH STREAM
	kinect.initDepthStream(true);

	// INITIALIZE THE BODY INDEX AS WELL
	kinect.initBodyIndexStream();

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

	// GRAB THE BODY INDEX PIXELS
	pxlBodyIndex = kinect.getBodyIndexPixelsRef();

	// RECREATE THE MESH EVERY UPDATE
	mesh.clear();
		
	// CREATE A BASIC POINT CLOUD BY SETTING THE POINT Z VALUE TO THE PROCESSED
	// GREYSCALE COLOR OF THE DEPTH IMAGE PER EACH X & Y PIXEL VALUE.
	for (int y = 0; y < DEPTH_HEIGHT; y++) {
		for (int x = 0; x < DEPTH_WIDTH; x++) {
			int index = x + (y*DEPTH_WIDTH);

			// CHECK AGAINST THE BODY INDEX IMAGE TO SEE IF THE X & Y ARE INSIDE OF A
			// FOUND BODY OUTLINE. IF NOT, THEN CONTINUE ITERATING THROUGH THE PIXELS.
			ofColor BodyIndexHit = pxlBodyIndex.getColor(x, y);
			if (BodyIndexHit.r > 100) continue;

			// OTHERWISE, PROCEED AS BEFORE
			float z = Z_AXIS_MAGNITUDE * pxlDepth.getPixels()[index];

			// IGNORE THE HIGH AND LOW VALUES
			if ((z == 0) || (z == Z_AXIS_MAGNITUDE) ) continue;

			// CREATE A POINT WITH THE SAME X AND Y BY A Z VALUE MATCHING THE DEPTH
			ofVec3f newPoint(x - DEPTH_WIDTH / 2, (DEPTH_HEIGHT / 2) - y, z - .5*Z_AXIS_MAGNITUDE);
			mesh.addVertex(newPoint);

			ofColor c = ofColor(255, 255, 255);
			mesh.addColor(c);
		}
	}

}

//--------------------------------------------------------------
void testApp::drawMesh() {
	camera.begin();
	ofPushMatrix();
	ofTranslate(0, 200, 0);
	ofScale(2.0, 2.0, 2.0);
	mesh.draw();
	ofPopMatrix();
	camera.end();
}

//--------------------------------------------------------------
void testApp::draw()
{
	ofBackground(24);

	ofSetColor(255);
	kinect.drawDepth(ofRectangle(10,10,DEPTH_WIDTH, DEPTH_HEIGHT));
	kinect.drawBodyIndex(10, DEPTH_HEIGHT + 20);
	
	drawMesh();
}
