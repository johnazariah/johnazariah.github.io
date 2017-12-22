#!/bin/bash
function emitTag {
    mkdir -p tags/$1
    echo "---"              >> tags/$1/index.html
	echo "layout : tagpage" >> tags/$1/index.html
	echo "tag : $1"         >> tags/$1/index.html
	echo "---"              >> tags/$1/index.html
}

rm -rf tags
grep tags _posts/* | gawk -F: '{print $3}' | sed -e "s/\[//g; s/\]//g" | gawk -F, '{for(i=1;i<=NF;i++) {printf "%s \n", $i}}' | sort | uniq | while read n; do emitTag $n; done
