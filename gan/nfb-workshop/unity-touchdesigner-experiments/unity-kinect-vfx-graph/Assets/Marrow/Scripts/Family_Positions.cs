using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Family_Positions : MonoBehaviour
{
    [System.Serializable]
    public class FamilyRow
    {
        public List<Transform> family;
    }

    public Transform framePosition;
    public List<Transform> creationPositions;
    private List<List<Vector3>> originalPositions = new List<List<Vector3>>();
    private Sequence animationSequence;

    [Header("Families")]
    public List<FamilyRow> familyRows;

    void Start()
    {
        if (familyRows == null)
        {
            Debug.LogError("Missing references for familyRows property!");
            return;
        }

        var rowIndex = 0;
        familyRows.ForEach((row) =>
        {
            originalPositions.Add(new List<Vector3>());

            row.family.ForEach(family =>
            {

                // Cache family's original position
                originalPositions[rowIndex].Add(family.position);

                // Move family behind the frame
                family.position = framePosition.position;

                family.gameObject.SetActive(false);
            });

            rowIndex++;
        });

        animationSequence = DOTween.Sequence().Pause();

        var activeRowIndex = 0;
        familyRows.ForEach(row =>
        {
            var rowTweens = DOTween.Sequence().Pause();

            var activeRow = familyRows[activeRowIndex];

            // Move to creation pvar activeRowEase = Ease.OutSine;osition
            var moveSpeed1 = 5;
            var activeRowEase1 = Ease.InOutSine;
            var family_1 = activeRow.family[0].DOMove(creationPositions[0].position, moveSpeed1).SetDelay(1f).SetEase(activeRowEase1).Pause();
            var family_2 = activeRow.family[1].DOMove(creationPositions[1].position, moveSpeed1).SetDelay(1f).SetEase(activeRowEase1).Pause();
            var family_3 = activeRow.family[2].DOMove(creationPositions[2].position, moveSpeed1).SetDelay(1f).SetEase(activeRowEase1).Pause();
            var family_4 = activeRow.family[3].DOMove(creationPositions[3].position, moveSpeed1).SetDelay(1f).SetEase(activeRowEase1).Pause();

            rowTweens.AppendCallback(() =>
            {
                activeRow.family[0].gameObject.SetActive(true);
            });
            rowTweens.Append(family_1);


            rowTweens.AppendCallback(() =>
            {
                activeRow.family[1].gameObject.SetActive(true);
            });
            rowTweens.Append(family_2);


            rowTweens.AppendCallback(() =>
            {
                activeRow.family[2].gameObject.SetActive(true);
            });
            rowTweens.Append(family_3);


            rowTweens.AppendCallback(() =>
            {
                activeRow.family[3].gameObject.SetActive(true);
            });
            rowTweens.Append(family_4);

            // Move to original position
            var moveSpeed2 = 6;
            var activeRowEase2 = Ease.InOutSine;
            var family_1_original_pos = activeRow.family[0].DOMove(originalPositions[activeRowIndex][0], moveSpeed2).SetEase(activeRowEase2).Pause();
            var family_2_original_pos = activeRow.family[1].DOMove(originalPositions[activeRowIndex][1], moveSpeed2).SetEase(activeRowEase2).Pause();
            var family_3_original_pos = activeRow.family[2].DOMove(originalPositions[activeRowIndex][2], moveSpeed2).SetEase(activeRowEase2).Pause();
            var family_4_original_pos = activeRow.family[3].DOMove(originalPositions[activeRowIndex][3], moveSpeed2).SetEase(activeRowEase2).Pause();

            var rowTweens_2 = DOTween.Sequence().Pause();

            rowTweens_2.Insert(0, family_1_original_pos);
            rowTweens_2.Insert(0, family_2_original_pos);
            rowTweens_2.Insert(0, family_3_original_pos);
            rowTweens_2.Insert(0, family_4_original_pos);

            // Save first row sequence
            animationSequence.Append(rowTweens);
            animationSequence.Append(rowTweens_2);

            activeRowIndex++;
        });

        // Debug start animation
        //Animate();
    }

    [Sirenix.OdinInspector.Button("Start Animation")]
    public void Animate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        animationSequence.Play();
    }


}
