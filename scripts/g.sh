#!/bin/bash
#
# Git Maintainer Script
# This script is intended to make it easy to handle a repo with multiple subfolders that are also repos.
# It is assumed that all subrepos are organized in the same way, in terms of remotes and branches.
#
# g.sh init <path> <url>: clones the url inits the path at the specified location
# g.sh status: show status information about all repos
# g.sh commit <message>: records and commits all changes for all affected repos
# g.sh branch <name>: creates the branch in every repo where it doesn't exist and switches to it
# g.sh branch -d <name>: deletes the specified branch in every repo.
# g.sh branch -r <oldname> <newname>: renames a branch in every repo.
# g.sh add <name> <url>: Creates a folder with the specified name and clones the specified repo into it. The branch that is checked out is the same as the current one in the main repo.
#
# g.sh add name url [remote] [branch]: creates a folder (subtree) with the specified name and populates it with the specified git repo
# g.sh push [remote] [branch]: pushes all subtrees and the main repo
# g.sh pull [remote] [branch]: pulls the main repo and all subtrees
#
# The remote and branch arguments apply to every subtree in the same way.
# If not specified, origin and master are the default values.
#
# Example:
#   g.sh pull origin master
#   pulls every subtree (and the main repo) from it's origin remote and checks out their respective master branches
#



TC='\x1b['
Green="${TC}32m";
Yellow="${TC}33m";
Red="${TC}31m";
Rst="${TC}0m";     # Reset all coloring and style
exe() { echo "\$ $@" ; "$@" ; }

if [ $# -lt 1 ]
then
    echo "${Yellow}$0: specify an option${Rst}"
    exit 1
fi


cmd=$1


# find all repos (it is assumed that they are sorted by path depth)
repos=`find . -type d -name '.git' | sed 's#\(.*\)/.*#\1#'`
nrepos=`echo "$repos" | wc -l`
actions=0


# Catches an error in the previous command.
# If an error occurred, the script quits and optionally performs a checkout on two git repos.
#  catch <message>: outputs a message on failure
#  catch <message> <repo1> <ref1> <repo2> <ref2>: reverts two repos to a specified state in case of failure (the first repo is checked out first)
catch() {
    if [[ $? != 0 ]]; then
	echo -e "${Red}$1${Rst}" >&2
	if [[ $# == 5 ]]; then
	    #echo "$@"
	    rollback $2 $3 $4 $5
	elif [[ $# -ne 1 ]]; then
	    echo -e "${Red}invalid number of arguments for catch${Rst}"
	fi
	exit 1
    fi
}


# rolls back two repos and exits with an error code
rollback() {
    if [[ $# -ne 4 ]]; then
	echo -e "${Red}invalid number of arguments for rollback${Rst}"
	exit 1
    else
	p1=`echo "$1" | sed -e 's/^"//' -e 's/"$//'`
	r1=`echo "$2" | sed -e 's/^"//' -e 's/"$//'`
	p2=`echo "$3" | sed -e 's/^"//' -e 's/"$//'`
	r2=`echo "$4" | sed -e 's/^"//' -e 's/"$//'`

	if [[ -n $no_rollback ]]; then
	    { (cd "$p1" && git checkout "$r1") } &> /dev/null
	    catch "Failed to roll back $p1 to $r1. Run git checkout $r1 on $p1 manually, then run git checkout $r2 on $p2 to restore state prior to script execution"
            { (cd "$p2" && git checkout "$r2") } &> /dev/null
	    catch "Failed to roll back $p2 to $r2. Run git checkout $r2 on $p2 manually to restore state prior to script execution."
	fi

	if [[ -n $new_branch ]]; then
	    { (cd "$p1" && git branch -D $new_branch) }
	    catch "Failed to delete newly created branch ($new_branch). To do it manually, go to $p1 and run git branch -d $new_branch."
	fi
    fi
    exit 1
}



# Checks if the current working directory of the specified repo is dirty with respect to another path.
# If there are no changes that affect the reference path, the directory is considered clean.
check_dirty() {
    status=`(cd "$1" && git status --porcelain) | sed 's/^[MADRCU ?!]\{2\}[[:space:]]\(.*\)$/.\/\1/'`
    catch "failed to retrieve status of $1"
    rel_path=`echo "$2" | sed -e 's/[]\/$*.^|[]/\\\&/g'`
    files=`echo "$status" | grep "^${rel_path}"`
    
    if [ -z "$files" ]; then
	return 0
    else
	return 1
    fi
}


# prints the name of the specified folder
folder_name() {
    # 1. print full path
    # 2. remove points, redundant slashes and trailing slash
    # 3. extract last path element
    echo $(cd $(dirname "$1") && pwd -P)/$(basename "$1") | sed -e 's/\/\./\//g' -e 's/\/\/*/\//g' -e 's/\/$//g' | sed 's/^.*\/\([^\/]*\)$/\1/'
}


# usage: sync <path> <branch>
# Synchronizes a repo with it's most direct parent repo. The sync is conducted on the specified branch
# This is done by rolling the parent back to the earliest commit that is newer than the newest commit of the child repo.
# If the child was also updated since then, a merge new branch is created in the child repo to reflect the changes that were applied to the parent.
# This new branch is then merged with the main (specified) branch.
sync() {
    branch=$2
    
    child_path=$1
    child_name=`folder_name "$child_path"`

    # Find parent repo
    parent_path=
    for repo in $repos; do
	if [[ "$1" == "$repo" ]]; then
	    continue
	elif [[ "$1" == "$repo"* ]]; then
	   parent_path=$repo
	fi
    done
    parent_name=`folder_name "$parent_path"`
    if [ -z "$parent_path" ]; then
	echo -e "${Red}could not find parent repo of $child_name${Rst}"
	exit 1
    fi

    echo -e "sync child $child_path with parent $parent_path on branch $branch"


    # Make sure that we'll be able to revert to the current state
    if [[ `(cd "$parent_path" && git status) | grep '^HEAD detached'` ]]; then
	echo -e "${Red}HEAD is detached in $parent_name. Check out a branch first.${Rst}"
	exit 1
    elif [[ `(cd "$child_path" && git status) | grep '^HEAD detached'` ]]; then
	echo -e "${Red}HEAD is detached in $child_name. Check out a branch first.${Rst}"
	exit 1
    fi
    
    #orig_p=`(cd "$parent_path" && git log) | sed -n -e 's/^commit \([A-Za-z0-9]*\)$/\1/p' | sed -e '1!d'`
    #orig_c=`(cd "$child_path" && git log) | sed -n -e 's/^commit \([A-Za-z0-9]*\)$/\1/p' | sed -e '1!d'`
    #check_dirty "$parent_path" .
    #if [ $? != 0 ]; then
	#echo -e "${Red}$child_name can't be synchronized because $parent_name is not clean. Commit changes and try again.${Rst}" >&2
	#exit 1
    #fi
    

    orig_p=`(cd "$parent_path" && git branch --list) | sed -n -e "s/\*[[:space:]]*\(.*\)/\1/p"`
    orig_c=`(cd "$child_path" && git branch --list) | sed -n -e "s/\*[[:space:]]*\(.*\)/\1/p"`

    parent_branches=`(cd "$parent_path" && git branch --list) | sed -n -e "s/[^ ]*[[:space:]]*\(.*\)/\1/p"`
    child_branches=`(cd "$child_path" && git branch --list) | sed -n -e "s/[^ ]*[[:space:]]*\(.*\)/\1/p"`
    
    catch_args=`echo "\"$child_path\" \"$orig_c\" \"$parent_path\" \"$orig_p\""`
    #echo -e "$catch_args"


    
    # Make sure the sync branch exists in parent repo
    if [[ -z `echo "$parent_branches" | grep "^$branch$"` ]]; then
	echo -e "${Red}the parent repo $parent_name does not have a branch named $branch${Rst}"
	exit 1
    fi
    
    
    # Make sure the sync branch exists in child repo (create if it doesn't exist)
    if [[ -z `echo "$child_branches" | grep "^$branch$"` ]]; then
	# Case 1: The branch on which to sync doesn't exist in the child repo. We'll create it from the latest commit where they had a common branch. This will later allow for fast forwarding.

	# Find latest common branch: in the parent repo, find the latest ancestor of the sync branch that also exists in the child repo
	{ (cd "$parent_path" && git checkout "$branch") } &> /dev/null
	catch "failed to checkout $branch in $parent_name" $catch_args
	child_branch_filter=`echo "$child_branches" | sed "s/^\(.*\)/-e '\^\1\$'/" | sed -e ':a' -e 'N' -e '$!ba' -e 's/\n/ /g'`
	latest_common_branch=`(cd "$parent_path" && git show-branch) | sed -n -e 's/^\*[^\[]*\[\([^~]*\).*[][].*/\1/p' | grep $child_branch_filter -m1`

	if [ -z "$latest_common_branch" ]; then
	   echo -e "${Red}None of the ancestor branches of $branch in $parent_name could be found in $child_name. You must create the branch manually.${Rst}"
	   rollback $catch_args
	fi


	echo -e "The branch $branch exists only in $parent_name. In $child_name, it will be created from branch $latest_common_branch."


	echo -e "${Red}not tested${Rst}"
	rollback $catch_args
	exit 1

	# From the latest common branch, we'll branch off the new sync branch in the child repo
	{ (cd "$child_path" && git checkout "$latest_common_branch" -b "$branch") } &> /dev/null
	catch "failed to create new branch $branch on $latest_common_branch in $child_name" $catch_args
			
	fork_point=`(cd "$parent_path" && git merge-base "$latest_common_branch" "$branch" --fork-point)`
        { (cd "$parent_path" && git checkout "$fork_point") } &> /dev/null
	catch "failed to checkout forkpoint ($fork_point) between $latest_common_branch and $branch in $parent_name" $catch_args

	# Apply commit to child repo
	{ (cd "$child_path" && git add -A) } &> /dev/null
	catch "failed to add changes to $child_name" $catch_args
        { (cd "$child_path" && git commit -m"created branch $branch (generated by script while syncing with $parent_repo)") } &> /dev/null
	catch "failed to commit on $child_name" $catch_args
	
    else
	# Case 2: The branch on which to sync does already exist in the child repo. We'll have to find the latest common commits.
	echo -e "The branch $branch exists in both $parent_name and $child_name."
    fi

    # Switch to the respective branches in both repos
    { (cd "$child_path" && git checkout "$branch") } &> /dev/null
    catch "failed to checkout $branch in $child_name" $catch_args
    { (cd "$parent_path" && git checkout "$branch") } &> /dev/null
    catch "failed to checkout $branch in $parent_name" $catch_args

    


    # read history of both child and parent
    parent_history=`(cd "$parent_path" && git log) | sed -n -e 's/^commit \([A-Za-z0-9]*\)$/\1/p'`
    child_history=`(cd "$child_path" && git log) | sed -n -e 's/^commit \([A-Za-z0-9]*\)$/\1/p'`
    parent_history_length=`echo "$parent_history" | wc -l | sed -n -e 's/^[[:space:]]*\([0-9]*\)[[:space:]]*$/\1/p'`
    child_history_length=`echo "$child_history" | wc -l | sed -n -e 's/^[[:space:]]*\([0-9]*\)[[:space:]]*$/\1/p'`
    parent_pos=1
    child_pos=1


    #echo "parent history ($parent_history_length): $parent_history"
    #echo "child history ($child_history_length): $child_history"
    

    # make sure that there actually exist commits
    if [ $parent_history_length -lt 1 ]; then
	echo -e "${Red}$parent_name has no commits yet${Rst}"
	rollback $catch_args
    fi
    if [ $child_history_length -lt 1 ]; then
	echo -e "${Red}$child_name has no commits yet${Rst}"
	rollback $catch_args
    fi

    parent_commit=`echo "$parent_history" | sed "$parent_pos!d"`
    parent_date=`(cd "$parent_path" && git show -s --format=%ct "$parent_commit")`
    child_commit=`echo "$child_history" | sed "$child_pos!d"`
    child_date=`(cd "$child_path" && git show -s --format=%ct "$child_commit")`
    

    echo "Searching latest common commits between $child_name ($child_history_length commits) and $parent_name ($parent_history_length commits)"


    while [ true ]; do

	echo "parent: commit $parent_pos ($parent_date), child: commit $child_pos ($child_date)"
	
	if [ $parent_date -lt $child_date ]; then
	    echo "reverse child"
	    
	    child_pos=$((child_pos+1))
	    if [ $child_history_length -lt $child_pos ]; then
		echo -e "${Red}Reached beginning of history of $child_name - no common ancestor found with $parent_name. Make the current state a common ancestor by committing $child_name.${Rst}"
		rollback $catch_args
	    fi
	    
	    child_commit=`echo "$child_history" | sed "$child_pos!d"`
	    child_date=`(cd "$child_path" | git show -s --format=%ct "$child_commit")`
	    
	    { (cd "$child" && git checkout "$child_commit") } &> /dev/null
	    catch "failed to checkout commit $child_commit in $child_name" $catch_args
	    
	    check_dirty "$parent_path" "$child_path"
	    if [ $? == 0 ]; then
		break
	    fi
	else
	    echo "reverse parent"
	    
	    parent_pos=$((parent_pos+1))
	    if [ $parent_history_length -lt $parent_pos ]; then
		echo -e "${Red}Reached beginning of history of $parent_name - no common ancestor found with $child_name. Make the current state a common ancestor by committing $parent_name.${Rst}"
		rollback $catch_args
	    fi
	    
	    parent_commit=`echo "$parent_history" | sed "$parent_pos!d"`
	    parent_date=`(cd "$parent_path" | git show -s --format=%ct "$parent_commit")`
	    
	    { (cd "$parent_path" && git checkout "$parent_commit") } &> /dev/null
	    catch "failed to checkout commit $parent_commit in $parent_name" $catch_args
	    
	    check_dirty "$child_path" "$parent_path"
	    if [ $? == 0 ]; then
		break
	    fi	    
	fi

    done
    
    echo "Latest common commits: $parent_commit in $parent_name and $child_commit in $child_name"


    # Create new branch in child repo to include all the commits that were made in the parent repo since the last common commit
    # Any rollback executed afterwards will delete the temporary branch
    { (cd "$child_path" && git checkout -b $parent_name-changes "$child_commit" ) } &> /dev/null
    catch "failed to create new branch ($parent_name-changes) on $child_name" $catch_args
    new_branch=$parent_name-changes
    
    commits_copied=0
    
    while [ $parent_pos -gt 1 ]; do
	# Switch to state of next commit
    	parent_pos=$((parent_pos-1))
	parent_commit=`echo "$parent_history" | sed "$parent_pos!d"`
	{ (cd "$parent_path" && git checkout "$parent_commit") } &> /dev/null
	catch "failed to checkout commit $parent_commit in $parent_name" $catch_args

	# Skip, if this commit didn't affect the child repo
	check_dirty "$child_path" "$parent_path"
	if [ $? == 0 ]; then
	    continue
	fi

	# Apply commit to child repo
	commit_message=`(cd "$parent_path" && git log -n 1 "$parent_commit" | sed -n -e 's/^    \(.*\)/\1/p')`
	commit_author=`(cd "$parent_path" && git log -n 1 "$parent_commit" | sed -n -e 's/^Author:[[:space:]]*\(.*\)/\1/p')`
	{ (cd "$child_path" && git add -A) } &> /dev/null
	catch "failed to add changes to $child_name" $catch_args
	{ (cd "$child_path" && git commit -m"$commit_message" --author="$commit_author") } &> /dev/null
	catch "failed to commit on $child_name" $catch_args
	    
	commits_copied=$((commits_copied+1))
    done

    echo -e "${Green}Applied $commits_copied commits to branch $new_branch${Rst}"

    # Checkout the branches on which the sync is running
    unset new_branch
    no_rollback=1
    { (cd "$parent_path" && git checkout $branch) } &> /dev/null
    catch "failed to checkout $branch in $parent_name" $catch_args
    { (cd "$child_path" && git checkout $branch) } &> /dev/null
    catch "failed to checkout $branch in $child_name" $catch_args
    
    # Merge the parent-changes-branch with the main branch
    echo -e "Merging changes back into child"
    { (cd "$child_path" && git merge --no-edit $parent_name-changes) } &> /dev/null
    catch "failed to merge $parent_name-changes into $branch in $child_name" $catch_args
    { (cd "$child_path" && git branch -d $parent_name-changes) } &> /dev/null
    catch "failed to delete merged branch $parent_name-changes from $child_name" $catch_args

    echo -e "${Red}Everything went well.${Rst}"


    # todo
    # 1. what happens if the automatic merge fails? the user must somehow commit the resolved conflicts
    # 2. how to integrate child changes into parent? how will this work for multiple children? maybe at very start, do an octopus merge of all "-changes" branches
    # 3. probably need to reverse order (start at children)
    # 4. when reversing order, the part where a branch is automatically created for the child, makes no sense
    

}


commit() {
    status=`(cd $1 && git status)`
    catch "failed to check status of $1"
    
    if [[ -z `echo "$status" | grep 'nothing to commit'` ]]; then
	echo -e "something to commit in $1"
	(cd $1 && exe git add -A)
	catch "failed to add changes to $1"
	(cd $1 && exe git commit -m"$2")
	catch "failed to commit changes in $1"
	actions=$((actions+1))
	
    else
	echo -e "nothing to commit in $1"
    fi
}




case $cmd in
    show)
	echo "found $nrepos repos"
        for repo in $repos
	do
	    name=`folder_name "$repo"`
	    branch=`(cd "$repo" && git branch --list) | sed -n -e "s/\*[[:space:]]*\(.*\)/\1/p"`
	    echo "repo $name (at $repo): on branch $branch"
	done;;

    sync)
	children=`echo "$repos" | tail -n+2`
	for repo in "$children"
	do
	    echo "sync $repo"
	    sync "$repo" "master"
	done;;

    push)
         echo -e "${Red}not implemented, this will push all repos to the server${Rst}"
	 exit 1;;
    
    pull)
        echo -e "${Red}not implemented, this will pull all repos from the server (children first)${Rst}"
	exit 1;;
    
    *)
	echo -e "${Yellow}$0: unknown command $1${Rst}" >&2
	exit 1;;
esac
