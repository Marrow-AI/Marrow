for i in results/*.wav;
  do name="$(basename -- $i)"
  echo "$name"
  ffmpeg -i "$i" "results_wav/${name}"
done
