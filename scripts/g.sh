#!/bin/bash
#
# Git Maintainer Script
#
# g.sh show
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


if [ $# -lt 1 ]
then
    echo "${Yellow}$0: specify an option${Rst}"
    exit 1
fi


fixedargs=0
if [[ $1 = add ]]
then
    fixedargs=2
    addname=$2
    addurl=$3
fi


remote=origin
if [ $# -gt $(($fixedargs+1)) ]
then
    if [$fixedargs = 0]
    then
	remote=$2
    elif [$fixedargs = 1]
    then
        remote=$3
    elif [$fixedargs = 2]
    then
	remote=$4
    fi
fi


branch=master
if [ $# -gt $(($fixedargs+2)) ]
then
    if [$fixedargs = 0]
    then
	remote=$3
    elif [$fixedargs = 1]
    then
        remote=$4
    elif [$fixedargs = 2]
    then
	remote=$5
    fi
fi


subtrees=`git log | grep git-subtree-dir | tr -d ' ' | cut -d ":" -f2 | sort | uniq | xargs -I {} bash -c 'if [ -d $(git rev-parse --show-toplevel)/{} ] ; then echo {}; fi'`
nsubtrees=`echo "$var" | wc -l`

case $1 in
    show)
        for subtree in $subtrees
	do
	    echo "found subtree: $subtree"
	done;;

    add)

	subtree=$addname
	git remote add -f $subtree-$remote $addurl
	git subtree add --prefix=$subtree $subtree-$remote $branch --squash
	;;
    
    push)
	for subtree in $subtrees
	do
	    echo "push subtree: $subtree"
	    git subtree push --prefix=$subtree $subtree-$remote $branch
	    if [[ $? != 0 ]]
	    then
		echo -e "${Red}pushing $subtree failed, try $0 pull first${Rst}"
		exit 1
	    fi
	done

	git push $remote $branch
	if [[ $? != 0 ]]
	then
	    echo -e "${Red}pushing main repo failed, try $0 pull first${Rst}"
	    exit 1
	fi
	
        echo -e "${Green}pushed $nsubtrees subtrees and main repo successfully${Rst}";;
    
    pull)
	git pull $remote $branch
	if [[ $? != 0 ]]
	then
	    echo -e "${Red}pulling main repo failed${Rst}"
	    exit 1
	fi
	
	for subtree in $subtrees
	do
	    echo "pull subtree: $subtree"
	    git subtree pull --prefix=$subtree $subtree-$remote $branch --squash
	    if [[ $? != 0 ]]
	    then
		echo -e "${Red}pulling $subtree failed${Rst}"
		exit 1
	    fi
	done
	
        echo -e "${Green}pulled main repo and $nsubtrees subtrees successfully${Rst}";;
    
    *)
	echo -e "${Yellow}$0: unknown command $1${Rst}" >&2
	exit 1;;
esac
