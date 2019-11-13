#!/bin/bash
while read line
do
  parts=( $line )
  echo "rm ${parts[0]}"
  rm ${parts[0]}
done < "${1:-/dev/stdin}"
