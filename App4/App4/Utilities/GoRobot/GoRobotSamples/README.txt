GoRobotSamples contains sample projects for using the GoRobot library for integration between Gocator sensors and KUKA robots.
Similar method can be used to integrate to any other robot brand, an implementation of GoRobotDriverInterface should be added for each robot type.

Requirements for using the KUKA sample projects:
* Kuka robot
* Gocator sensor mounted on the robot end of arm (Line profiler or Snapshot), or mounted on a fixed position (Snapshot sensors only)
* KUKA.EthernetKRL application installed on the robot controller.

In order to use GoRobot's Kuka Examples, configuration the Kuka robot should be done:

* XmlServerFollow.xml
  Is the KUKA.EthernetKRL configuration file, and should be placed in the C:\KRC\ROBOTER\Config\User\Common\EthernetKRL folder on the robot.
* XmlLinearFollower.src
  Is the program which must be selected and running on the Kuka robot for that communication with the PC would be possible.

These files are available in the GoRobot\GoRobotSamples\Resource folder.
Please refer to the Kuka robot documentation and the KUKA.EthernetKRL documentation for more details.
