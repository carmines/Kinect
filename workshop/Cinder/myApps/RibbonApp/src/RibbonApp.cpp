#include "cinder/app/AppNative.h"
#include "cinder/gl/gl.h"
#include "cinder/gl/Vbo.h"
#include "cinder/Rand.h"

using namespace ci;
using namespace ci::app;
using namespace std;

#include "Kinect2.h"

class RibbonApp : public ci::app::AppBasic
{
public:
	void setup();
	void prepareSettings( ci::app::AppBasic::Settings* settings );
	void update();
    void draw();

private:
	Kinect2::DeviceRef			mDevice;
    Kinect2::BodyFrame			mBodyFrame;

    UINT64 mTrackingID;
    Vec3f mHand;
    std::vector<Vec3f> mPoints;
};

void RibbonApp::prepareSettings( Settings* settings )
{
	settings->prepareWindow( Window::Format().size( 1600, 800 ).title( "Ribbon" ) );
	settings->setFrameRate( 60.0f );
}

void RibbonApp::setup()
{
    // setup kinect
	mDevice = Kinect2::Device::create();
	mDevice->start();
	mDevice->connectBodyEventHandler( [ & ]( const Kinect2::BodyFrame frame )
	{
		mBodyFrame = frame;
	} );

    Rand::randomize();

    mTrackingID = 0;
    mHand = Vec3f(0, 0, 0);
    mPoints.push_back(mHand);
}

void RibbonApp::update()
{
    // get body data from Kinect
    const std::vector<Kinect2::Body> svBodies = mBodyFrame.getBodies();
    
    // if we are not tracking someone find a new one
    if (mTrackingID == 0)
    {
        for (const Kinect2::Body& body : svBodies) {
            if (body.isTracked()) {
                mTrackingID = body.getId();
                mPoints.clear();

                // set the first point to the user hand position
                const map<JointType, Kinect2::Body::Joint> joints = body.getJointMap();
                auto joint = joints.find(JointType::JointType_HandRight);
                if (joint == joints.end())
                {
                    break;
                }

                if (joint->second.getTrackingState() != TrackingState::TrackingState_NotTracked)
                {
                    mHand = joint->second.getPosition();
                }
                mPoints.push_back(mHand);

                break;
            }
        }
    }

    // get the hand for the tracked person
    bool bTracking = false;
    for (const Kinect2::Body& body : svBodies) {
        if (body.isTracked() && mTrackingID == body.getId()) {
            bTracking = true; // flag that we are still tracking

            const map<JointType, Kinect2::Body::Joint> joints = body.getJointMap();
            auto joint = joints.find(JointType::JointType_HandRight);
            if (joint == joints.end())
            {
                continue;
            }

            // may want to add that the hand state is close for example
            if (joint->second.getTrackingState() == TrackingState::TrackingState_Tracked) {
                Vec3f pos = joint->second.getPosition();
                mHand += joint->second.getPosition() * 60.0f;
                mHand *= .94f;
                mHand.y *= -1.0f;
                mHand.z = pos.z;

                if (mPoints.size() < 100)
                {
                    mPoints.push_back(mHand);
                }
                else
                {
                    mPoints.erase(mPoints.cbegin()); // remove first item
                }
            }
        }
    }

    // no one to track
    if (mTrackingID == 0 || !bTracking)
    {
        mTrackingID = 0;

        mHand += Rand::randVec3f() * 60.0;
        mHand *= .94;

        if (mPoints.size() > 100)
        {
            // remove first item
            mPoints.erase(mPoints.cbegin());
        }
        else
        {
            mPoints.push_back(mHand);
        }

        return;
    }

}

void RibbonApp::draw()
{
    gl::clear();
    gl::enableAdditiveBlending();

    gl::pushMatrices();
    gl::translate(getWindowCenter());

    //gl::begin(GL_POINTS);
    gl::begin(GL_QUAD_STRIP);

    int _tailLength = mPoints.size();
    float radius = 30.0f;

    for (int i = _tailLength - 2; i > 0; i--)
    {
        float per = 0.0 + (float)i / (float)(_tailLength - 1);

        Vec3f perp0 = mPoints[i] - mPoints[i + 1];

        Vec3f perp1 = perp0.cross(Vec3f::yAxis());

        Vec3f perp2 = perp0.cross(perp1);

        perp1 = perp0.cross(perp2).normalized();

        Vec3f off = perp1 * (radius * per);

        //gl::color(per, 1.0f, per * 0.5f, 1.0f);
        gl::color(per, per * 0.25f, 1.0f - per, per * 0.5f);

        gl::vertex(mPoints[i] - off);

        gl::vertex(mPoints[i] + off);
    }

    glEnd();

    gl::popMatrices();
}

CINDER_APP_BASIC( RibbonApp, RendererGl )
