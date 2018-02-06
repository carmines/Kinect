#include "ofMain.h"
#include "testApp.h"
#ifdef TARGET_OPENGLES
#include "ofGLProgrammableRenderer.h"
#endif

//========================================================================
int main( ){

//	ofSetLogLevel(OF_LOG_VERBOSE);
	#ifdef TARGET_OPENGLES
		ofSetCurrentRenderer(ofGLProgrammableRenderer::TYPE);
	#endif

	ofSetupOpenGL(1920,1080, OF_WINDOW);

	ofRunApp( new testApp());

}
