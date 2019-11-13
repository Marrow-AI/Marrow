for file in curated/*
do
 python ~/Marrow/remove_bg/remove_bg.py $file curated-nobg/$(basename "$file")
done
