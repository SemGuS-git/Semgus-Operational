(declare-term-types
    ;; Nonterminals
    ((Start 0) (R 0))
    
    ;; Productions
    (
        (($eval($eval_1 R)))
    
        (
            ($eps)
            ($phi)
            ($char_1)
                ($char_2)
                ($char_3)
                ($char_4)
                ($char_5)
                ($char_6)
                ($char_7)
                ($char_8)
                ($char_9)
                ($char_10)
                ($any)
            ($or ($or_1 R) ($or_2 R))
            ($concat ($concat_1 R) ($concat_2 R))
            ;; ($star ($star_1 R))
        )
    )
)

(define-funs-rec
    (
        (Start.Sem ((t Start) (s_0 Int) (s_1 Int) (s_2 Int) (s_3 Int) (s_4 Int) (result Bool)) Bool)
        (R.Sem ((t R) (s_0 Int) (s_1 Int) (s_2 Int) (s_3 Int) (s_4 Int) (X_0_0 Bool) (X_0_1 Bool) (X_0_2 Bool) (X_0_3 Bool) (X_0_4 Bool) (X_0_5 Bool) (X_1_1 Bool) (X_1_2 Bool) (X_1_3 Bool) (X_1_4 Bool) (X_1_5 Bool) (X_2_2 Bool) (X_2_3 Bool) (X_2_4 Bool) (X_2_5 Bool) (X_3_3 Bool) (X_3_4 Bool) (X_3_5 Bool) (X_4_4 Bool) (X_4_5 Bool) (X_5_5 Bool)) Bool)
    )
    
    (
        (! (match t (
            (($eval t1) (exists
                ( (X_0_0 Bool) (X_0_1 Bool) (X_0_2 Bool) (X_0_3 Bool) (X_0_4 Bool) (X_0_5 Bool) (X_1_1 Bool) (X_1_2 Bool) (X_1_3 Bool) (X_1_4 Bool) (X_1_5 Bool) (X_2_2 Bool) (X_2_3 Bool) (X_2_4 Bool) (X_2_5 Bool) (X_3_3 Bool) (X_3_4 Bool) (X_3_5 Bool) (X_4_4 Bool) (X_4_5 Bool) (X_5_5 Bool))
                (and
                    (R.Sem t1 s_0 s_1 s_2 s_3 s_4 X_0_0 X_0_1 X_0_2 X_0_3 X_0_4 X_0_5 X_1_1 X_1_2 X_1_3 X_1_4 X_1_5 X_2_2 X_2_3 X_2_4 X_2_5 X_3_3 X_3_4 X_3_5 X_4_4 X_4_5 X_5_5)
                    (= result X_0_5)
                )
            ))
        )) :input ( s_0 s_1 s_2 s_3 s_4) :output (result))
        (! (match t (
            ($eps (and  (= X_0_0 true) (= X_0_1 false) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 true) (= X_1_2 false) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 true) (= X_2_3 false) (= X_2_4 false) (= X_2_5 false) (= X_3_3 true) (= X_3_4 false) (= X_3_5 false) (= X_4_4 true) (= X_4_5 false) (= X_5_5 true)))
            ($phi (and  (= X_0_0 false) (= X_0_1 false) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 false) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 false) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 false) (= X_3_5 false) (= X_4_4 false) (= X_4_5 false) (= X_5_5 false)))
            ($any (and  (= X_0_0 false) (= X_0_1 true) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 true) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 true) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 true) (= X_3_5 false) (= X_4_4 false) (= X_4_5 true) (= X_5_5 false)))
                ($char_1 (and  (= X_0_0 false) (= X_0_1 (= s_0 1)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 1)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 1)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 1)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 1)) (= X_5_5 false)))
                ($char_2 (and  (= X_0_0 false) (= X_0_1 (= s_0 2)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 2)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 2)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 2)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 2)) (= X_5_5 false)))
                ($char_3 (and  (= X_0_0 false) (= X_0_1 (= s_0 3)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 3)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 3)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 3)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 3)) (= X_5_5 false)))
                ($char_4 (and  (= X_0_0 false) (= X_0_1 (= s_0 4)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 4)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 4)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 4)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 4)) (= X_5_5 false)))
                ($char_5 (and  (= X_0_0 false) (= X_0_1 (= s_0 5)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 5)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 5)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 5)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 5)) (= X_5_5 false)))
                ($char_6 (and  (= X_0_0 false) (= X_0_1 (= s_0 6)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 6)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 6)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 6)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 6)) (= X_5_5 false)))
                ($char_7 (and  (= X_0_0 false) (= X_0_1 (= s_0 7)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 7)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 7)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 7)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 7)) (= X_5_5 false)))
                ($char_8 (and  (= X_0_0 false) (= X_0_1 (= s_0 8)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 8)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 8)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 8)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 8)) (= X_5_5 false)))
                ($char_9 (and  (= X_0_0 false) (= X_0_1 (= s_0 9)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 9)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 9)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 9)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 9)) (= X_5_5 false)))
                ($char_10 (and  (= X_0_0 false) (= X_0_1 (= s_0 10)) (= X_0_2 false) (= X_0_3 false) (= X_0_4 false) (= X_0_5 false) (= X_1_1 false) (= X_1_2 (= s_1 10)) (= X_1_3 false) (= X_1_4 false) (= X_1_5 false) (= X_2_2 false) (= X_2_3 (= s_2 10)) (= X_2_4 false) (= X_2_5 false) (= X_3_3 false) (= X_3_4 (= s_3 10)) (= X_3_5 false) (= X_4_4 false) (= X_4_5 (= s_4 10)) (= X_5_5 false)))
                (($or t1 t2)
                (exists
                    (
                         (A_0_0 Bool) (A_0_1 Bool) (A_0_2 Bool) (A_0_3 Bool) (A_0_4 Bool) (A_0_5 Bool) (A_1_1 Bool) (A_1_2 Bool) (A_1_3 Bool) (A_1_4 Bool) (A_1_5 Bool) (A_2_2 Bool) (A_2_3 Bool) (A_2_4 Bool) (A_2_5 Bool) (A_3_3 Bool) (A_3_4 Bool) (A_3_5 Bool) (A_4_4 Bool) (A_4_5 Bool) (A_5_5 Bool)
                         (B_0_0 Bool) (B_0_1 Bool) (B_0_2 Bool) (B_0_3 Bool) (B_0_4 Bool) (B_0_5 Bool) (B_1_1 Bool) (B_1_2 Bool) (B_1_3 Bool) (B_1_4 Bool) (B_1_5 Bool) (B_2_2 Bool) (B_2_3 Bool) (B_2_4 Bool) (B_2_5 Bool) (B_3_3 Bool) (B_3_4 Bool) (B_3_5 Bool) (B_4_4 Bool) (B_4_5 Bool) (B_5_5 Bool)
                    )
                    (and 
                        (R.Sem t1 s_0 s_1 s_2 s_3 s_4 A_0_0 A_0_1 A_0_2 A_0_3 A_0_4 A_0_5 A_1_1 A_1_2 A_1_3 A_1_4 A_1_5 A_2_2 A_2_3 A_2_4 A_2_5 A_3_3 A_3_4 A_3_5 A_4_4 A_4_5 A_5_5)
                        (R.Sem t2 s_0 s_1 s_2 s_3 s_4 B_0_0 B_0_1 B_0_2 B_0_3 B_0_4 B_0_5 B_1_1 B_1_2 B_1_3 B_1_4 B_1_5 B_2_2 B_2_3 B_2_4 B_2_5 B_3_3 B_3_4 B_3_5 B_4_4 B_4_5 B_5_5)
                        (and
                            (= X_0_0 (or A_0_0 B_0_0))
                            (= X_0_1 (or A_0_1 B_0_1))
                            (= X_0_2 (or A_0_2 B_0_2))
                            (= X_0_3 (or A_0_3 B_0_3))
                            (= X_0_4 (or A_0_4 B_0_4))
                            (= X_0_5 (or A_0_5 B_0_5))
                            (= X_1_1 (or A_1_1 B_1_1))
                            (= X_1_2 (or A_1_2 B_1_2))
                            (= X_1_3 (or A_1_3 B_1_3))
                            (= X_1_4 (or A_1_4 B_1_4))
                            (= X_1_5 (or A_1_5 B_1_5))
                            (= X_2_2 (or A_2_2 B_2_2))
                            (= X_2_3 (or A_2_3 B_2_3))
                            (= X_2_4 (or A_2_4 B_2_4))
                            (= X_2_5 (or A_2_5 B_2_5))
                            (= X_3_3 (or A_3_3 B_3_3))
                            (= X_3_4 (or A_3_4 B_3_4))
                            (= X_3_5 (or A_3_5 B_3_5))
                            (= X_4_4 (or A_4_4 B_4_4))
                            (= X_4_5 (or A_4_5 B_4_5))
                            (= X_5_5 (or A_5_5 B_5_5))
                        )
                    )
                )
            )
            (($concat t1 t2)
                (exists
                    (
                         (A_0_0 Bool) (A_0_1 Bool) (A_0_2 Bool) (A_0_3 Bool) (A_0_4 Bool) (A_0_5 Bool) (A_1_1 Bool) (A_1_2 Bool) (A_1_3 Bool) (A_1_4 Bool) (A_1_5 Bool) (A_2_2 Bool) (A_2_3 Bool) (A_2_4 Bool) (A_2_5 Bool) (A_3_3 Bool) (A_3_4 Bool) (A_3_5 Bool) (A_4_4 Bool) (A_4_5 Bool) (A_5_5 Bool)
                         (B_0_0 Bool) (B_0_1 Bool) (B_0_2 Bool) (B_0_3 Bool) (B_0_4 Bool) (B_0_5 Bool) (B_1_1 Bool) (B_1_2 Bool) (B_1_3 Bool) (B_1_4 Bool) (B_1_5 Bool) (B_2_2 Bool) (B_2_3 Bool) (B_2_4 Bool) (B_2_5 Bool) (B_3_3 Bool) (B_3_4 Bool) (B_3_5 Bool) (B_4_4 Bool) (B_4_5 Bool) (B_5_5 Bool)
                    )
                    (and 
                        (R.Sem t1 s_0 s_1 s_2 s_3 s_4 A_0_0 A_0_1 A_0_2 A_0_3 A_0_4 A_0_5 A_1_1 A_1_2 A_1_3 A_1_4 A_1_5 A_2_2 A_2_3 A_2_4 A_2_5 A_3_3 A_3_4 A_3_5 A_4_4 A_4_5 A_5_5)
                        (R.Sem t2 s_0 s_1 s_2 s_3 s_4 B_0_0 B_0_1 B_0_2 B_0_3 B_0_4 B_0_5 B_1_1 B_1_2 B_1_3 B_1_4 B_1_5 B_2_2 B_2_3 B_2_4 B_2_5 B_3_3 B_3_4 B_3_5 B_4_4 B_4_5 B_5_5)
                        (and
                            (= X_0_0 (and A_0_0 B_0_0))
                            (= X_0_1 (or (and A_0_0 B_0_1) (and A_0_1 B_1_1)))
                            (= X_0_2 (or (and A_0_0 B_0_2) (and A_0_1 B_1_2) (and A_0_2 B_2_2)))
                            (= X_0_3 (or (and A_0_0 B_0_3) (and A_0_1 B_1_3) (and A_0_2 B_2_3) (and A_0_3 B_3_3)))
                            (= X_0_4 (or (and A_0_0 B_0_4) (and A_0_1 B_1_4) (and A_0_2 B_2_4) (and A_0_3 B_3_4) (and A_0_4 B_4_4)))
                            (= X_0_5 (or (and A_0_0 B_0_5) (and A_0_1 B_1_5) (and A_0_2 B_2_5) (and A_0_3 B_3_5) (and A_0_4 B_4_5) (and A_0_5 B_5_5)))
                            (= X_1_1 (and A_1_1 B_1_1))
                            (= X_1_2 (or (and A_1_1 B_1_2) (and A_1_2 B_2_2)))
                            (= X_1_3 (or (and A_1_1 B_1_3) (and A_1_2 B_2_3) (and A_1_3 B_3_3)))
                            (= X_1_4 (or (and A_1_1 B_1_4) (and A_1_2 B_2_4) (and A_1_3 B_3_4) (and A_1_4 B_4_4)))
                            (= X_1_5 (or (and A_1_1 B_1_5) (and A_1_2 B_2_5) (and A_1_3 B_3_5) (and A_1_4 B_4_5) (and A_1_5 B_5_5)))
                            (= X_2_2 (and A_2_2 B_2_2))
                            (= X_2_3 (or (and A_2_2 B_2_3) (and A_2_3 B_3_3)))
                            (= X_2_4 (or (and A_2_2 B_2_4) (and A_2_3 B_3_4) (and A_2_4 B_4_4)))
                            (= X_2_5 (or (and A_2_2 B_2_5) (and A_2_3 B_3_5) (and A_2_4 B_4_5) (and A_2_5 B_5_5)))
                            (= X_3_3 (and A_3_3 B_3_3))
                            (= X_3_4 (or (and A_3_3 B_3_4) (and A_3_4 B_4_4)))
                            (= X_3_5 (or (and A_3_3 B_3_5) (and A_3_4 B_4_5) (and A_3_5 B_5_5)))
                            (= X_4_4 (and A_4_4 B_4_4))
                            (= X_4_5 (or (and A_4_4 B_4_5) (and A_4_5 B_5_5)))
                            (= X_5_5 (and A_5_5 B_5_5))
                            
                        )
                    )
                )
            )
            ;; (($star t1)
            ;;     (exists
            ;;         (
            ;;              (A_0_0 Bool) (A_0_1 Bool) (A_0_2 Bool) (A_0_3 Bool) (A_0_4 Bool) (A_0_5 Bool) (A_1_1 Bool) (A_1_2 Bool) (A_1_3 Bool) (A_1_4 Bool) (A_1_5 Bool) (A_2_2 Bool) (A_2_3 Bool) (A_2_4 Bool) (A_2_5 Bool) (A_3_3 Bool) (A_3_4 Bool) (A_3_5 Bool) (A_4_4 Bool) (A_4_5 Bool) (A_5_5 Bool)
            ;;         )
            ;;         (and 
            ;;             (R.Sem t1 s_0 s_1 s_2 s_3 s_4 A_0_0 A_0_1 A_0_2 A_0_3 A_0_4 A_0_5 A_1_1 A_1_2 A_1_3 A_1_4 A_1_5 A_2_2 A_2_3 A_2_4 A_2_5 A_3_3 A_3_4 A_3_5 A_4_4 A_4_5 A_5_5)
                        
            ;;             (and
            ;;             (= X_0_0 true)
            ;;             (= X_0_1 A_0_1)
            ;;             (= X_0_2 (or A_0_2 (and A_0_1 A_1_2)))
            ;;             (= X_0_3 (or A_0_3 (and A_0_2 A_2_3) (and A_0_1 A_1_3) (and A_0_1 A_1_2 A_2_3)))
            ;;             (= X_0_4 (or A_0_4 (and A_0_3 A_3_4) (and A_0_2 A_2_4) (and A_0_2 A_2_3 A_3_4) (and A_0_1 A_1_4) (and A_0_1 A_1_3 A_3_4) (and A_0_1 A_1_2 A_2_4) (and A_0_1 A_1_2 A_2_3 A_3_4)))
            ;;             (= X_0_5 (or A_0_5 (and A_0_4 A_4_5) (and A_0_3 A_3_5) (and A_0_3 A_3_4 A_4_5) (and A_0_2 A_2_5) (and A_0_2 A_2_4 A_4_5) (and A_0_2 A_2_3 A_3_5) (and A_0_2 A_2_3 A_3_4 A_4_5) (and A_0_1 A_1_5) (and A_0_1 A_1_4 A_4_5) (and A_0_1 A_1_3 A_3_5) (and A_0_1 A_1_3 A_3_4 A_4_5) (and A_0_1 A_1_2 A_2_5) (and A_0_1 A_1_2 A_2_4 A_4_5) (and A_0_1 A_1_2 A_2_3 A_3_5) (and A_0_1 A_1_2 A_2_3 A_3_4 A_4_5)))
                        
            ;;             (= X_1_1 true)
            ;;             (= X_1_2 A_1_2)
            ;;             (= X_1_3 (or A_1_3 (and A_1_2 A_2_3)))
            ;;             (= X_1_4 (or A_1_4 (and A_1_3 A_3_4) (and A_1_2 A_2_4) (and A_1_2 A_2_3 A_3_4)))
            ;;             (= X_1_5 (or A_1_5 (and A_1_4 A_4_5) (and A_1_3 A_3_5) (and A_1_3 A_3_4 A_4_5) (and A_1_2 A_2_5) (and A_1_2 A_2_4 A_4_5) (and A_1_2 A_2_3 A_3_5) (and A_1_2 A_2_3 A_3_4 A_4_5)))
                        
            ;;             (= X_2_2 true)
            ;;             (= X_2_3 A_2_3)
            ;;             (= X_2_4 (or A_2_4 (and A_2_3 A_3_4)))
            ;;             (= X_2_5 (or A_2_5 (and A_2_4 A_4_5) (and A_2_3 A_3_5) (and A_2_3 A_3_4 A_4_5)))
                        
            ;;             (= X_3_3 true)
            ;;             (= X_3_4 A_3_4)
            ;;             (= X_3_5 (or A_3_5 (and A_3_4 A_4_5)))
                        
            ;;             (= X_4_4 true)
            ;;             (= X_4_5 A_4_5)
                        
            ;;             (= X_5_5 true)
                        
            ;;             )
            ;;         )
            ;;     )
            ;; )
        )) :input ( s_0 s_1 s_2 s_3 s_4) :output ( X_0_0 X_0_1 X_0_2 X_0_3 X_0_4 X_0_5 X_1_1 X_1_2 X_1_3 X_1_4 X_1_5 X_2_2 X_2_3 X_2_4 X_2_5 X_3_3 X_3_4 X_3_5 X_4_4 X_4_5 X_5_5))
    )
)

(synth-fun match_regex() Start)

(constraint (Start.Sem match_regex 1 1 1 1 1 true))
(constraint (Start.Sem match_regex 1 1 1 1 2 false))
(constraint (Start.Sem match_regex 1 1 3 1 1 false))
(constraint (Start.Sem match_regex 2 4 1 2 1 false))
(constraint (Start.Sem match_regex 2 3 2 6 2 false))

(check-synth)

