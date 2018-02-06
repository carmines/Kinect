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

	pxlsMappedColor.allocate(512, 424, OF_IMAGE_COLOR_ALPHA);

	// INITIALIZE THE BODY INDEX
	kinect.initBodyIndexStream();

	// INITIALIZE THE COLOR STREAM
	kinect.initColorStream();

	//glPointSize(5);

	// START THE COMMON BRIDGE
	kinect.start();

	shader.load("shader/shader");

	ofDisableArbTex();
	ofLoadImage(texture, "pointBlur.png");
}

//--------------------------------------------------------------
void testApp::update(){

	kinect.update();
	if (kinect.isFrameNew())
	{
		updateMesh();
	}
}

//--------------------------------------------------------------
void testApp::updateMesh() 
{
	vbo.clear();

	points.clear();
	sizes.clear();
	colors.clear();

	// GRAB THE PROCESSED DEPTH PIXELS
    pxlDepth = kinect.getDepthPixelsRef();

	// GRAB THE BODY INDEX PIXELS
	pxlBodyIndex = kinect.getBodyIndexPixelsRef();

	kinect.mapDepthToColor(pxlsMappedColor);
	//kinect.altMapColorToDepth(pxlsMappedColor);

	texColorMappedToDepth.loadData(pxlsMappedColor);

	// GRAB THE MAPPED COLORS FOR DEPTH
	// ************************************************
	// NOTE: THIS IS AN EXPENSIVE OPERATION.
	// LOOK INTO KCBv2 TO DETERMINE MORE OPTIMIZED
	// SOLUTION. ALTERNATIVE WOULD BE TO SEND RAW
	// ARRAY OF DEPTH TO COLOR MAPPING TO GPU AND PERFORM
	// LOOK UPS THERE, ETC.
	//pxlColorMappedToDepth = kinect.getColorPixelsMappedToDepth();
	//texColorMappedToDepth.loadData(pxlColorMappedToDepth.getPixels(), 512, 424, GL_RGBA);
		
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
			//  pxlDepth.getPixels()
			float z = Z_AXIS_MAGNITUDE * pxlDepth.getPixels()[index];

//			float z = Z_AXIS_MAGNITUDE * kinect.getDepthPixelsRef().getPixels()[index];

			// IGNORE THE HIGH AND LOW VALUES
			if ((z == 0) || (z == Z_AXIS_MAGNITUDE) ) continue;

			// CREATE A POINT WITH THE SAME X AND Y BY A Z VALUE MATCHING THE DEPTH
			ofVec3f newPoint(x - DEPTH_WIDTH / 2, (DEPTH_HEIGHT / 2) - y, z - .5*Z_AXIS_MAGNITUDE);
			points.push_back(newPoint);

			//float percY = sin(PI*(y/(float) DEPTH_HEIGHT));
			float percY = sin(4.0*(y / (float) DEPTH_HEIGHT));

			float size = 6.0*percY;
			sizes.push_back(ofVec3f(size));

			//ofColor c = pxlColorMappedToDepth.getColor(x, y);
			ofColor c = pxlsMappedColor.getColor(x, y);
			colors.push_back(ofFloatColor(c.r / 255.0f, c.g / 255.0f, c.b / 255.0f, percY));
		}
	}

	int total = (int) points.size();
	if (total > 0)
	{
		vbo.setVertexData(&points[0], total, GL_STATIC_DRAW);
		vbo.setColorData(&colors[0], total, GL_STATIC_DRAW);
		vbo.setNormalData(&sizes[0], total, GL_STATIC_DRAW);
	}

}

//--------------------------------------------------------------
void testApp::drawMesh() {

	//ofEnableBlendMode(OF_BLENDMODE_ADD);
	ofEnablePointSprites();

	shader.begin();
	camera.begin();
	ofPushMatrix();

	ofScale(1.25, 1.25, 1.5);
	texture.bind();
	vbo.draw(GL_POINTS, 0, (int) points.size());
	texture.unbind();

	ofPopMatrix();
	camera.end();
	shader.end();

	ofDisablePointSprites();
	//ofDisableBlendMode();

}

//--------------------------------------------------------------
void testApp::draw()
{
	ofBackground(0);

	glDepthMask(GL_FALSE);
	ofSetColor(255);

	drawMesh();

	kinect.drawDepth(ofRectangle(10,10,DEPTH_WIDTH, DEPTH_HEIGHT));
	texColorMappedToDepth.draw(ofRectangle(10, 20 + DEPTH_HEIGHT, DEPTH_WIDTH, DEPTH_HEIGHT));

	ofDrawBitmapString("FPS: " + ofToString(ofGetFrameRate()), ofPoint(10, 40 + (DEPTH_HEIGHT * 2)));
	
}
