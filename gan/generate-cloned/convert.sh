for i in results/*.wav;
  do name="$(basename -- $i)"
  echo "$name"
  ffmpeg -y -i "$i" "/home/avnerus/temp/in-ear/${name}"
done
