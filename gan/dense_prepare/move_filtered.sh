#!/bin/bash
while read line
do
  parts=( $line )
  mv "dense_out/${parts[0]}" dense_filtered/
done < "${1:-/dev/stdin}"
