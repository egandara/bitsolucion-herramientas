# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_005_Base_Normativa_Modelo_Consumo_B1
# MAGIC **************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC ***************************************************************************************************************
# MAGIC * Nombre: BCI_005_Base_Normativa_Modelo_Consumo_B1.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor: BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de generar el modelo consumo Estandar e Interno
# MAGIC * Documentación: 
# MAGIC ***************************************************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tablas Entrada: 
# MAGIC * slv_RiesgoCred_RiesgoCredPer_db.tbl_prov_gr_con_bci_ope_0722
# MAGIC * slv_RiesgoCred_RiesgoCredPer_db.prov_gr_con_bci_ope_b1_0125
# MAGIC ***************************************************************************
# MAGIC #### Tablas Salida: 
# MAGIC * tmp_modelo_consumo

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variable

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Capturar de Variables

# COMMAND ----------

dbutils.widgets.removeAll()

dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso:")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

dbutils.widgets.text("db_location_plat_tempW","","06 platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)


db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)


print("Parámetros")
print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_slv_ParametricasX,'db_slv_ParametricasX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')
valida_bd(db_slv_ParametricasX, 'db_slv_ParametricasX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC #### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignacion Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)
ano_mes_dia = str(ano+'-'+mes+'-01')

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)
print("fecha_Formato8: " + ano_mes_dia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio de Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ### Creacion Tablas Temporales

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla bci05_basenorma_tmp_modelo_consumo

# COMMAND ----------

drop_modelo_consumo_int = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.bci05_basenorma_tmp_modelo_consumo"""

# COMMAND ----------

sql_safe(drop_modelo_consumo_int)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"bci05_basenorma_tmp_modelo_consumo", True)

# COMMAND ----------

create_modelo_consumo_int = """CREATE TABLE """ + db_plat_tempX + """.bci05_basenorma_tmp_modelo_consumo
(
  periodo_id				  STRING,
  colocacion_id	      STRING,
  cliente_id	        STRING,
  deuda_totalifrs	    DECIMAL(30, 6),
  prov_totalactiva	  DECIMAL(30, 6),
  deudatot_cont	      DECIMAL(30, 6),
  provtot_cont	      DECIMAL(30, 6),
  cuenta_ctb 	        STRING,
  pi		              DECIMAL(30, 6),
  pdi		              DECIMAL(30, 6),
  pe                  DECIMAL(30, 6),
  modelo              STRING
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""bci05_basenorma_tmp_modelo_consumo' """

# COMMAND ----------

sql_safe(create_modelo_consumo_int)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Tabla tmp_modelo_consumo

# COMMAND ----------

drop_tmp_modelo_consumo = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_modelo_consumo"""

# COMMAND ----------

sql_safe(drop_tmp_modelo_consumo)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_modelo_consumo", True)

# COMMAND ----------

create_tmp_modelo_consumo = """CREATE TABLE """ + db_plat_tempX + """.tmp_modelo_consumo
(
  periodo_id				      INT,
  cod_num_operacion	      STRING,
  cliente_rut	            INT,
  mnt_deuda_totalifrs	    DECIMAL(30, 6),
  mnt_prov_totalactiva	  DECIMAL(30, 6),
  mnt_deudatot_cont	      DECIMAL(30, 6),
  mnt_provtot_cont	      DECIMAL(30, 6),
  cod_cuenta_ctb 	        STRING,
  pct_pi		              DECIMAL(30, 6),
  pct_pdi		              DECIMAL(30, 6),
  pct_pe                  DECIMAL(30, 6),
  cod_modelo              STRING,
  fec_proceso             DATE
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_modelo_consumo' """

# COMMAND ----------

sql_safe(create_tmp_modelo_consumo)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Desarrollo Logica

# COMMAND ----------

ult_dia_mes = ultimo_dia_habil(db_slv_ParametricasX, anomes)

ult_dia = ult_dia_mes.toPandas()
fec_proceso = ult_dia.iloc[0]["periodo_id"]

ano_proceso = str(fec_proceso)[:4]
mes_proceso = str(fec_proceso)[4:][:2]
dia_proceso = str(fec_proceso)[6:][:2]
date_proceso = str(ano_proceso+'-'+mes_proceso+'-'+dia_proceso)
date_proceso_2 = str(ano_proceso+mes_proceso+dia_proceso)

print("fecha_Formato9: " + date_proceso)
print("fecha_Formato10: " + date_proceso_2)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 1: Obtencion Univ Modelo Consumo Interno

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del modelo consumo interno desde tbl_prov_gr_con_bci_ope_0722
# MAGIC
# MAGIC **Tabla de salida**: bci05_basenorma_tmp_modelo_consumo_int (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMMDD			
# MAGIC - **colocacion_id**: Codigo operacion
# MAGIC - **cliente_id**: Rut del cliente
# MAGIC - **deuda_totalifrs**: Monto total deuda IFRS
# MAGIC - **prov_totalactiva**: Monto total provision activa
# MAGIC - **deudatot_cont**: Monto total deuda contingente
# MAGIC - **provtot_cont**: Monto total provision contingente
# MAGIC - **cuenta_ctb**: Numero cuenta contable
# MAGIC - **pi**: Porcentaje probabilidad de incumplimiento
# MAGIC - **pdi**: Porcentaje perdida dada por el incumplimiento
# MAGIC - **pe**: Porcentaje perdida esperada
# MAGIC - **modelo**: Identificador tipo de modelo

# COMMAND ----------

vista_univ_modelo_consumo_int = """ insert into """ + db_plat_tempX + """.bci05_basenorma_tmp_modelo_consumo
  select  fec_fpro	    as periodo_id,
          operacion     as colocacion_id,
          rut           as cliente_id,
          sdo_total		  as Deuda_TotalIFRS,
          prv_tot_act		as Prov_TotalActiva,
          sdo_tot_ctg		as DeudaTot_Cont,
          prv_tot_ctg		as ProvTot_Cont,
          ctb_d00			  as cuenta_ctb,
          cast(round(val_pi/100,5) as decimal(30, 6))       as pi,
          cast(round(val_pdi/100,5) as decimal(30, 6))      as pdi,
          cast(round(tas_ope/100,5) as decimal(30, 6))      as pe,
          'INT'                                             as modelo
  from """ + db_RiesgoCredX + """.tbl_prov_gr_con_bci_ope_0722
  where fec_fpro = '""" + date_proceso_2 + """'
    and (sdo_total + sdo_tot_ctg) > 0
"""

# COMMAND ----------

sql_safe(vista_univ_modelo_consumo_int)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 2: Obtencion Univ Modelo Consumo Estandar

# COMMAND ----------

# MAGIC %md
# MAGIC Se crea una vista para generar la informacion del modelo consumo estandar desde tbl_prov_gr_con_bci_ope_0722
# MAGIC
# MAGIC **Tabla de salida**: bci05_basenorma_tmp_modelo_consumo (vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMMDD
# MAGIC - **colocacion_id**: Codigo operacion
# MAGIC - **cliente_id**: Rut del cliente
# MAGIC - **deuda_totalifrs**: Monto total deuda IFRS
# MAGIC - **prov_totalactiva**: Monto total provision activa
# MAGIC - **deudatot_cont**: Monto total deuda contingente
# MAGIC - **provtot_cont**: Monto total provision contingente
# MAGIC - **cuenta_ctb**: Numero cuenta contable
# MAGIC - **pi**: Porcentaje probabilidad de incumplimiento
# MAGIC - **pdi**: Porcentaje perdida dada por el incumplimiento
# MAGIC - **pe**: Porcentaje perdida esperada
# MAGIC - **modelo**: Identificador tipo de modelo

# COMMAND ----------

vista_univ_modelo_consumo_std = """ insert into """ + db_plat_tempX + """.bci05_basenorma_tmp_modelo_consumo
  select  fec_fpro	    as periodo_id,
          operacion     as colocacion_id,
          rut           as cliente_id,
          sdo_total		  as Deuda_TotalIFRS,
          prv_tot_act		as Prov_TotalActiva,
          sdo_tot_ctg		as DeudaTot_Cont,
          prv_tot_ctg		as ProvTot_Cont,
          ctb_d00			  as cuenta_ctb,
          cast(round(val_pi/100,5) as decimal(30, 6))       as pi,
          cast(round(val_pdi/100,5) as decimal(30, 6))      as pdi,
          cast(round(tas_ope/100,5) as decimal(30, 6))      as pe,
          'STD'                                             as modelo
  from """ + db_RiesgoCredX + """.prov_gr_con_bci_ope_b1_0125
  where fec_fpro = '""" + date_proceso_2 + """'
    and (sdo_total + sdo_tot_ctg) > 0
"""

# COMMAND ----------

sql_safe(vista_univ_modelo_consumo_std)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 3: Eliminacion Datos Tabla 

# COMMAND ----------

# MAGIC %md
# MAGIC Se elimina los datos de la tabla tmp_modelo_consumo correspondiente a la fecha de proceso que se va a insertar posteriormente
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_consumo (tabla de salida)

# COMMAND ----------

delete_tmp_modelo_consumo = """delete from """ + db_plat_tempX + """.tmp_modelo_consumo where periodo_id = """ + anomes + """ """

# COMMAND ----------

sql_safe(delete_tmp_modelo_consumo)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Paso 4: Insercion Datos Tabla tmp_modelo_consumo

# COMMAND ----------

# MAGIC %md
# MAGIC Se prepara e inserta los datos a la tabla tmp_modelo_consumo desde la vista bci05_basenorma_tmp_modelo_consumo
# MAGIC
# MAGIC **Tabla de salida**: tmp_modelo_consumo (tabla de salida)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: Fecha de cierre AAAAMMDD
# MAGIC - **cod_num_operacion**: Codigo operacion
# MAGIC - **cliente_rut**: Rut del cliente
# MAGIC - **mnt_deuda_totalifrs**: Monto total deuda IFRS
# MAGIC - **mnt_prov_totalactiva**: Monto total provision activa
# MAGIC - **mnt_deudatot_cont**: Monto total deuda contingente
# MAGIC - **mnt_provtot_cont**: Monto total provision contingente
# MAGIC - **cod_cuenta_ctb**: Numero cuenta contable
# MAGIC - **pct_pi**: Porcentaje probabilidad de incumplimiento
# MAGIC - **pct_pdi**: Porcentaje perdida dada por el incumplimiento
# MAGIC - **pct_pe**: Porcentaje perdida esperada
# MAGIC - **cod_modelo**: Identificador tipo de modelo	

# COMMAND ----------

insert_tmp_modelo_consumo = """ insert into """ + db_plat_tempX + """.tmp_modelo_consumo
  select  substring(periodo_id, 1, 6)       as periodo_id,
          colocacion_id                     as cod_num_operacion,
          cliente_id                        as cliente_rut,
          Deuda_TotalIFRS                   as mnt_deuda_totalifrs,
          Prov_TotalActiva                  as mnt_prov_totalactiva,
          DeudaTot_Cont                     as mnt_deudatot_cont,
          ProvTot_Cont                      as mnt_provtot_cont,
          cuenta_ctb                        as cod_cuenta_ctb,
          pi                                as pct_pi,
          pdi                               as pct_pdi,
          pe                                as pct_pe,
          modelo                            as cod_modelo,			     
          '""" + date_proceso + """'        as fec_proceso
  from """ + db_plat_tempX + """.bci05_basenorma_tmp_modelo_consumo
"""

# COMMAND ----------

sql_safe(insert_tmp_modelo_consumo)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_plat_tempX+""".tmp_modelo_consumo  where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_modelo_consumo ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")