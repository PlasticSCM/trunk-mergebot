# Trunk mergebot

The trunk mergebot is an implementation of a Trunk-Based Development DevOps cycle
as we understand it in the Plastic SCM team.

This is the source code used by the actual built-in mergebot. Use it as a reference
to build your own mergebot!

# Build
The executable is built from .NET Framework code using the provided `src/trunk-mergebot.sln`
solution file. You can use Visual Studio or MSBuild to compile it.

**Note:** We'll use `${DEVOPS_DIR}` as alias for `%PROGRAMFILES%\PlasticSCM5\server\devops`
in *Windows* or `/var/lib/plasticscm/devops` in *macOS* or *Linux*.

# Setup
If you just want to use the built-in trunk mergebot you don't need to do any of this.

## Configuration files
You'll notice some configuration files under `/src/configuration`. Here's what they do:
* `trunkbot.log.conf`: log4net configuration. The output log file is specified here. This file should be in the binaries output directory.
* `trunkbot.definition.conf`: mergebot definition file. You'll need to place this file in the Plastic SCM DevOps directory to allow the system to discover your trunkbot.
* `trunkbot.config.template`: mergebot configuration template. It describes the expected format of the trunkbot configuration. We recommend to keep it in the binaries output directory
* `trunkbot.conf`: an example of a valid trunkbot configuration. It's built according to the `trunkbot.config.template` specification.

## Add to Plastic SCM Server DevOps
To allow Plastic SCM Server DevOps to discover your custom trunkbot, just drop 
the `trunkbot.definition.conf` file in `${DEVOPS_DIR}/config/mergebots/available$`.
Make sure the `command` and `template` keys contain the appropriate values for
your deployment! Your custom mergebot will be listed in the mergebot types page of
the WebAdmin under the "Custom" section.

# Behavior
The trunk mergebot monitors branches in a given repository. When they are marked as 
resolved in Plastic SCM and the related issue in the configured Issue Tracker
System is done, the trunk mergebot temporarily merges that branch in the configured
trunk branch and triggers a build in the configured Continuous Integration system.

If the build succeeds, the trunk mergebot commits the temporary merge the trunk branch.
Otherwise, it removes the temporary merge. It also notifies the user about the result
of the branch processing.

# Support
If you have any questions about this mergebot don't hesitate to contact us by
[email](support@codicesoftware.com) or in our [forum](http://www.plasticscm.net)!