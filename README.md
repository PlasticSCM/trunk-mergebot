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
The trunkbot is available as a built-in mergebot in the DevOps section of the WebAdmin.
Open it up and configure your own!

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
The **Trunk Bot**, by its **mergebot** nature, is the central piece of the DevOps
orchestration process. It's connected to the Plastic SCM Server, waiting for branches
to be set to **resolved**. It also retrieves task information from **issue trackers**,
triggers builds in external **CI systems** and it's able to **notify** the team
about the progress and results of these operations.

<p align="center">
  <img alt="DevOps diagram" src="https://raw.githubusercontent.com/PlasticSCM/trunk-mergebot/master/doc/img/devops-diagram.png" />
</p>

**Trunk Bot** is our take on the [trunk-based development](https://trunkbaseddevelopment.com/)
methodology. It's engineered to monitor many short-lived feature branches (we also
call them *task branches*) and automatically merge them when the developers finish
implementing the feature and the resulting CI builds are successful. This changes
the definition of *Done*, which now means *delivered to production*.

The first step begins when a user completes a task assigned to them. The related
task branch receives a value for the `status` attribute to mark it as `resolved`.
The information in the issue tracker updates as well to reflect that advance in
the task lifecycle and notify a fellow developer that it's time for the code review.

This attribute change sends a message to **Trunk Bot**. The resolved branch is now
queued and pending to be processed. However, the bot won't take any further action
until the code review is complete. This is when the **Issue tracker plug** comes
into play: **Trunk Bot** will periodically poll the task issue to find out if the
code review is complete. The code reviewer just needs to set the appropriate state
in the task lifecycle to unlock the next step for **Trunk Bot**.

When the code review is complete and the branch is properly identified as `resolved`
in Plastic SCM, **Trunk Bot** will start a CI build using the **CI plug**. While
the task branch is processing, its status will change to `testing` in both Plastic
SCM (new value of the `status` attribute) and the configured Issue Tracker.

**Trunk Bot** ensures that the trunk branch stability is never broken. It temporarily
merges the task branch into the trunk branch and runs the CI builds in the resulting
shelve. So, if the CI build fails, the shelve would be removed and the merge would
roll back. No changes are committed to the repository. Also, the task status in the
issue tracker and the `status` attribute would be set to `failed` to notify the result.

<p align="center">
  <img alt="Temporary merge" src="https://raw.githubusercontent.com/PlasticSCM/trunk-mergebot/master/doc/img/temporary-merge.png" />
</p>

However, if the merge operation isn't automatic (i.e. there are directory structure
conflicts or file conflicts that can't be automatically solved), **Trunk Bot**
considers that the branch processing failed, and it prompts the assigned developer
to solve the merge conflicts before the branch is again set as `resolved`. The
rate of automatic merges is improved thanks to our awesome [SemanticMerge](https://www.semanticmerge.com/) technology!

Finally, if there aren't any merge conflicts and the build results were correct,
then **Trunk Bot** attempts to commit the temporary merge to the trunk branch.
The shelve is promoted to changeset and your trunk is enhanced with the new feature
and a guaranteed stability! The task issue and the branch `status` attribute will
be set to a new `merged` value to show the progress in its lifecycle.

That's all! **Trunk bot** will work tirelessly monitoring your branches so that
you and your team can focus on getting things done while the Plastic SCM DevOps
ecosystem handles builds and merges. You might find interesting our
[blogpost about DevOps](http://blog.plasticscm.com/2018/03/plasticscm-devops-primer.html), too!

# Support
If you have any questions about this mergebot don't hesitate to contact us by
[email](support@codicesoftware.com) or in our [forum](http://www.plasticscm.net)!
